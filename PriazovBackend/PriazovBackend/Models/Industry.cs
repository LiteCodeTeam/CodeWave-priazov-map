using System.Runtime.InteropServices;

namespace DataBase.Models
{
    internal class Industry
    {
        public UInt32 Id { get; set; }
        public string Name { get; set; } = null!;

        List<Project> projects { get; set; } = null!;
    }       
}
