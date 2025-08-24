using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PaymentService.Models;

namespace PaymentService.Data
{
    public class PaymentServiceContext : DbContext
    {
        public PaymentServiceContext (DbContextOptions<PaymentServiceContext> options)
            : base(options)
        {
        }

        public DbSet<PaymentService.Models.Payment> Payment { get; set; } = default!;
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>()
                .Property(p=> p.Status)
                .HasConversion<string>();
            modelBuilder.Entity<Payment>()
                .Property(p => p.Method)
                .HasConversion<string>();
        }
    }
}
