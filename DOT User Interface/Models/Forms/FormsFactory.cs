namespace DOT_User_Interface.Models.Forms
{
    public class FormsFactory
    {
        public List<Forms> List = [];
        public Forms? WorkingForms;
        public void Cancel() => WorkingForms = null;
        public void Delete()
        {
            if (WorkingForms != null)
                List.Remove(WorkingForms);
            WorkingForms = null;
        }
        public void Save()
        {
            if (WorkingForms != null)
                List.Add(WorkingForms);
            WorkingForms = null;
        }
        public void Create()
        {
            WorkingForms = new();
        }
        public void Edit(Forms Forms)
        {
            WorkingForms = Forms;
        }
    }
}
