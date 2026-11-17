namespace DOT_User_Interface.Models.Tables
{
    public class Row
    {
        public Row(int size)
        {
            Size = size;
            Items = Enumerable.Repeat(string.Empty, size).ToList();
        }

        public Row(int size, List<string> src)
        {
            Size = size;
            // garante exatamente Size itens
            Items = (src ?? new List<string>())
                    .Concat(Enumerable.Repeat(string.Empty, size))
                    .Take(size)
                    .ToList();
        }

        public int Size { get; }

        public List<string> Items { get; set; }

        public Row Clone() => new(Size, new List<string>(Items));

        public string this[int index]
        {
            get
            {
                if (index < 0 || index >= Size) return string.Empty; // evita exceção
                return Items[index];
            }
            set
            {
                if (index < 0 || index >= Size) return;             // ignora fora do range
                Items[index] = value ?? string.Empty;
            }
        }
    }
}
