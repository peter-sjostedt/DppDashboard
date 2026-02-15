using System.Windows.Input;

namespace HospitexDPP.Models
{
    public class ActionCard
    {
        public string Icon { get; set; } = "";
        public string Title { get; set; } = "";
        public int Count { get; set; }
        public string Description { get; set; } = "";
        public ICommand? NavigateCommand { get; set; }
    }
}
