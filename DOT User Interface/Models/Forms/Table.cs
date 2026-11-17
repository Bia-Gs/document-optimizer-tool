namespace DOT_User_Interface.Models.Forms
{
    public class Table : Text, IForms
    {
        public Table()
        {
            Justification = Justification.Center;
        }
        public new string Name { get; set; } = string.Empty;
        public new FormsType Type { get; } = FormsType.Table;
        public int Width { get; set; }
        public int RowsHeight { get; set; }
        public List<Row> Rows { get; set; } = [];
        public Border Border { get; set; }
        public BorderStyle BorderStyle { get; set; }
        public void NewRow() => Rows.Add(new(this));
        public void RemoveRow(Row row) => Rows.Remove(row);
        public int NumberOfColumns { get; set; } = 2;
        public Shading Shading { get; set; } = new();
    }

    public class Row : Text
    {
        public Row(Table parent)
        {
            Parent = parent;   
        }
        private Table Parent { get; set; }
        public bool NoProof { get; set; }
        private int? height;
        public int Height
        {
            get
            {
                if (height.HasValue)
                    return height.Value;
                if (Parent != null && Parent.RowsHeight > 0)
                    return Parent.RowsHeight;

                return 0;
            }
            set => height = value;
        }
        private List<Cell> cells { get; set; } = [];
        public List<Cell> Cells
        {
            get
            {
                int required = Parent.NumberOfColumns;

                while (cells.Count < required)
                    cells.Add(new Cell(this));

                if (cells.Count > required)
                    cells = cells.Take(required).ToList();

                return cells;
            }
        }
        public Shading shading { get; set; } = new();
        public Shading Shading { get; set; } = new();
    }

    public class Cell : Text
    {
        public Cell(Row parent)
        {
            Parent = parent;
        }
        private new Row Parent { get; set; }
        public int Width { get; set; }
        public int Height => Parent.Height;
        public int GridColumn { get; set; }
        public Merge Merge { get; set; }
        public Border Border { get; set; }
        public BorderStyle BorderStyle { get; set; }
        public List<Text> Texts { get; set; } = [];
        public Shading shading { get; set; } = new();
        public Shading Shading { get; set; } = new();
        public Spacing Spacing { get; set; } = new();
    }
}
