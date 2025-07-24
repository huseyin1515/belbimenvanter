using System.Collections.Generic;
using System.Linq;
namespace BelbimEnv.Models
{
    public class RackUnitViewModel
    {
        public int U_Number { get; set; }
        public List<Server> OccupyingServers { get; set; } = new List<Server>();
        public bool IsOccupied => OccupyingServers.Any();
    }
}