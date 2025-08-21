using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CatalogService.Models;

namespace CatalogService.Data
{
    public class CatalogServiceContext : DbContext
    {
        public CatalogServiceContext (DbContextOptions<CatalogServiceContext> options)
            : base(options)
        {
        }

        public DbSet<CatalogService.Models.Book> Book { get; set; } = default!;
    }
}
