namespace DOT_User_Interface.Models.Forms
{
    public class Forms
    {
        public string Name { get; set; } = "New Forms";
        public string Description { get; set; } = "New Forms";
        public List<IForms> Items { get; } = [];
        public IForms? SelectedItem { get; set; }

        public void Add(FormsType Type)
        {
            Items.Add(Type switch
            {
                FormsType.Text => new Text(),
                FormsType.Table => new Table(),
                _ => throw new ArgumentOutOfRangeException(nameof(Type))
            });
        }
        public void Remove(IForms Item) => Items.Remove(Item);
        public void Edit(IForms Item) => SelectedItem = Item;
        public void Cancel() => SelectedItem = null;
    }
}
