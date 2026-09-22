using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    /// <summary>
    /// Marker for entities whose UpdatedAt is stamped automatically by AppDbContext.SaveChanges.
    /// </summary>
    public interface IUpdatable
    {
        DateTime? UpdatedAt { get; set; }
    }
}