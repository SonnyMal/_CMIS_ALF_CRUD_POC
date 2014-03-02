
namespace AlfrescoPlayAround
{
    class Program
    {
        static void Main(string[] args)
        {
            var alfMan = new AlfrescoManager("admin", "admin");

            //Rep
            var rep = alfMan.GetRepositories()[0];
            //Create
            var doc = alfMan.Create(rep, "shared");
            // Read
            var document = alfMan.Get(rep, doc.ContentStreamFileName);
            //Update
            alfMan.Update(rep, "shared", document);
            //Delete
            alfMan.Delete(rep, document);
        }
    }
}
