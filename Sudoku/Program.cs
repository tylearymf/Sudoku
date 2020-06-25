using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Sudoku
{
    class Cell
    {
        public Map Map { set; get; }

        public int Value { set; get; }

        public HashSet<int> Notes { set; get; }

        public int RowIndex { set; get; }
        public int ColumnIndex { set; get; }
        public int NineCellIndex { set; get; }

        public Cell(int v)
        {
            Value = v;
            Notes = new HashSet<int>();
        }

        public void UpdateNotes()
        {
            if (Value != 0) return;
            var notes = Notes;
            notes.Clear();

            var tempRows = Extensions.CellReverseValues(Map.RowsDic[RowIndex]);
            foreach (var item in tempRows)
            {
                notes.Add(item);
            }

            var tempColumns = Extensions.CellValues(Map.ColumnsDic[ColumnIndex]);
            foreach (var item in tempColumns)
            {
                notes.Remove(item);
            }

            var tempNineCells = Extensions.CellValues(Map.NineCellsDic[NineCellIndex]);
            foreach (var item in tempNineCells)
            {
                notes.Remove(item);
            }

            if (notes.Count > 1)
            {
                var nineCells = Map.NineCellsDic[NineCellIndex];
                foreach (var note in notes)
                {
                    var tempCells = new List<Cell>(nineCells);
                    tempCells.RemoveAll(x => !x.CanSet(note));

                    if (tempCells.Count == 1)
                    {
                        tempCells[0].Value = note;
                    }
                }
            }

            if (notes.Count == 1)
            {
                Value = notes.First();
            }
        }

        public bool CanSet(int value)
        {
            if (Value != 0) return false;
            var rowCells = Map.RowsDic[RowIndex];
            var columnCells = Map.ColumnsDic[ColumnIndex];

            var newList = rowCells.Concat(columnCells).ToList();
            var result = newList.Find(x => x.Value == value) != null;
            if (result) return false;

            return true;
        }

        public bool IsError()
        {
            if (Value == 0 && Notes.Count == 0)
            {
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            //if (Value != 0)
            {
                return Value.ToString();
            }

            return $"[{string.Join(",", Notes)}]";
        }

        public static implicit operator int(Cell v)
        {
            return v.Value;
        }

        public static implicit operator Cell(int v)
        {
            return new Cell(v);
        }
    }

    class Map
    {
        public const int MaxCount = 9;
        public const int NineCellCount = 3;

        public Cell[,] Cells { private set; get; }
        public Dictionary<int, List<Cell>> RowsDic { private set; get; }
        public Dictionary<int, List<Cell>> ColumnsDic { private set; get; }
        public Dictionary<int, List<Cell>> NineCellsDic { private set; get; }

        public bool LogEnable { set; get; } = false;

        public Map(string[] stringArray)
        {
            var rowLen = stringArray.Length;
            var columnLen = stringArray[0].Length;
            var cells = new Cell[rowLen, columnLen];

            for (int rowIndex = 0; rowIndex < rowLen; rowIndex++)
            {
                var columns = stringArray[rowIndex].ToCharArray();
                for (int columnIndex = 0; columnIndex < columnLen; columnIndex++)
                {
                    cells[rowIndex, columnIndex] = int.Parse(columns[columnIndex].ToString());
                }
            }

            Cells = cells;

            RowsDic = new Dictionary<int, List<Cell>>();
            ColumnsDic = new Dictionary<int, List<Cell>>();
            NineCellsDic = new Dictionary<int, List<Cell>>();
        }

        public Map(Cell[,] cells)
        {
            Cells = cells;

            RowsDic = new Dictionary<int, List<Cell>>();
            ColumnsDic = new Dictionary<int, List<Cell>>();
            NineCellsDic = new Dictionary<int, List<Cell>>();
        }

        public void InitAndAnalyze()
        {
            Init();
            Analyze();
        }

        void Init()
        {
            for (int row = 0; row < Cells.GetLength(0); row++)
            {
                var list = new List<Cell>();
                for (int column = 0; column < Cells.GetLength(1); column++)
                {
                    list.Add(Cells[row, column]);
                }
                list.ForEach(x => x.RowIndex = row);
                RowsDic.Add(row, list);
            }

            for (int column = 0; column < Cells.GetLength(1); column++)
            {
                var list = new List<Cell>();
                for (int row = 0; row < Cells.GetLength(0); row++)
                {
                    list.Add(Cells[row, column]);
                }
                list.ForEach(x => x.ColumnIndex = column);
                ColumnsDic.Add(column, list);
            }

            void AddNineCell(int rowIndex, int columIndex)
            {
                var list = new List<Cell>();

                for (int row = rowIndex * NineCellCount; row < (rowIndex + 1) * NineCellCount; row++)
                {
                    for (int column = columIndex * NineCellCount; column < (columIndex + 1) * NineCellCount; column++)
                    {
                        var cell = Cells[row, column];
                        cell.Map = this;
                        list.Add(cell);
                    }
                }

                var index = rowIndex * NineCellCount + columIndex;
                list.ForEach(x => x.NineCellIndex = index);
                NineCellsDic.Add(index, list);
            }

            for (int i = 0; i < NineCellCount; i++)
            {
                for (int j = 0; j < NineCellCount; j++)
                {
                    AddNineCell(i, j);
                }
            }
        }

        void Analyze()
        {
            var change = false;

            while (!HasResult())
            {
                change = false;

                for (int row = 0; row < Cells.GetLength(0); row++)
                {
                    for (int column = 0; column < Cells.GetLength(1); column++)
                    {
                        var cell = Cells[row, column];
                        if (cell.Value != 0) continue;

                        cell.UpdateNotes();

                        if (cell.Value != 0)
                        {
                            change = true;
                            break;
                        }
                    }

                    if (change)
                    {
                        break;
                    }
                }

                //尝试记录的值
                if (!change)
                {
                    if (this.HasError())
                    {
                        if (LogEnable)
                        {
                            Console.WriteLine("解析错误");
                        }
                        break;
                    }

                    var cell = ForeachCells(x => x.Value == 0 && x.Notes.Count > 1);
                    var notes = cell.Notes.ToList();
                    var noteIndex = 0;

                    while (noteIndex < notes.Count)
                    {
                        var tempMap = this.Deepcopy();
                        tempMap.Cells[cell.RowIndex, cell.ColumnIndex].Value = notes[noteIndex];
                        tempMap.InitAndAnalyze();

                        if (tempMap.HasError())
                        {
                            noteIndex++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    cell.Value = notes[noteIndex];
                }
            }
        }

        public bool HasError()
        {
            var cell = ForeachCells(x => x.IsError());
            return cell != null;
        }

        public bool HasResult()
        {
            var cell = ForeachCells(x => x.Value == 0);
            return cell == null;
        }

        public Cell ForeachCells(Predicate<Cell> predicate)
        {
            for (int row = 0; row < Cells.GetLength(0); row++)
            {
                for (int column = 0; column < Cells.GetLength(1); column++)
                {
                    var cell = Cells[row, column];
                    if (predicate(cell))
                    {
                        return cell;
                    }
                }
            }

            return null;
        }

        public Map Deepcopy()
        {
            var cells = new Cell[this.Cells.GetLength(0), this.Cells.GetLength(1)];
            for (int row = 0; row < this.Cells.GetLength(0); row++)
            {
                for (int column = 0; column < this.Cells.GetLength(1); column++)
                {
                    cells[row, column] = this.Cells[row, column].Value;
                }
            }

            var map = new Map(cells);
            return map;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            for (int i = 0; i < Cells.GetLength(0); i++)
            {
                for (int j = 0; j < Cells.GetLength(1); j++)
                {
                    var cell = Cells[i, j];
                    sb.Append($" {cell} ");

                    if (j == 2 || j == 5)
                    {
                        sb.Append(" | ");
                    }
                }

                if (i == 2 || i == 5)
                {
                    sb.AppendLine();
                }

                sb.AppendLine();
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    class Extensions
    {
        static public readonly HashSet<int> s_CellValue = new HashSet<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        static public HashSet<int> CellValues(List<Cell> cells)
        {
            var list = new HashSet<int>();
            foreach (var item in cells)
            {
                if (item.Value != 0)
                {
                    list.Add(item.Value);
                }
            }

            return list;
        }

        static public HashSet<int> CellReverseValues(List<Cell> cells)
        {
            var temp = CellValues(cells);
            var list = new HashSet<int>(s_CellValue);
            list.RemoveWhere(x => temp.Contains(x));

            return list;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var map = new Map(new string[Map.MaxCount]
            {
                "002080315",
                "040001000",
                "500000000",
                "460900501",
                "010700800",
                "093000070",
                "054200006",
                "026007400",
                "000006007",
            });

            map.LogEnable = true;
            map.InitAndAnalyze();

            Console.WriteLine(map);
        }

    }
}
