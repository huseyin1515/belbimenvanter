// Models/RackUnitViewModel.cs
namespace BelbimEnv.Models
{
    public class RackUnitViewModel
    {
        public int U_Number { get; set; }
        public Server OccupyingServer { get; set; } // Bu U birimini işgal eden sunucu
        public bool IsOccupied => OccupyingServer != null;
        public bool IsFront { get; set; } // Ön taraf mı, arka taraf mı?
    }
}