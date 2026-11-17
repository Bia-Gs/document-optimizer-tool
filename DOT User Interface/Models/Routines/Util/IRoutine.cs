namespace DOT_User_Interface.Models.Routines.Util
{
    public interface IRoutine
    {
        string Name { get; set; }
        string Description { get; set; }
        string Endpoint { get; }
        RoutineTypes Type { get; }
        IRoutine CloneBase();
        Task ShareAsync(I18n I18n);
        string ProcessingArgs(bool applyFilter = false);
    }
}
