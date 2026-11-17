namespace DOT_User_Interface.Models.Tables.Util
{
    public interface ITable
    {
        string Name { get; set; }
        string Description { get; set; }
        string Filter { get; set; }
        TableTypes Type { get; }
        IEnumerable<object> RowsAsObjects { get; }
        void AddRow();
        void RemoveRow(object row);
        ITable CloneBase();
        Task ShareAsync(I18n I18n);
        string ProcessingArgs(bool applyFilter = false);
    }
}
