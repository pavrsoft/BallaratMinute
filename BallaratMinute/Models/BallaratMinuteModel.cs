using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Entity.Spatial;
using Newtonsoft.Json.Linq;

namespace BallaratMinute.Models
{
    using System;
    using System.Data.Entity;
    using System.Linq;

    public class BallaratMinuteModel : DbContext
    {
        // Your context has been configured to use a 'BallaratMinuteModel' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'BallaratMinute.Models.BallaratMinuteModel' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'BallaratMinuteModel' 
        // connection string in the application configuration file.
        public BallaratMinuteModel()
            : base("name=BallaratMinuteModel")
        {
        }

        // Add a DbSet for each entity type that you want to include in your model. For more information 
        // on configuring and using a Code First model, see http://go.microsoft.com/fwlink/?LinkId=390109.

        public virtual DbSet<PointOfInterest> PointsOfInterest { get; set; }
        public virtual DbSet<POIType> POITypes { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PointOfInterest>().HasKey(i => i.ID);
            modelBuilder.Entity<POIType>().HasKey(i => i.ID);

            modelBuilder.Entity<POIType>().HasMany(i => i.POIs).WithRequired(i => i.Type).HasForeignKey(i => i.TypeID);
        }
    }

    public class PointOfInterest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }
        public string Name { get; set; }
        public string Desc { get; set; }
        public long TypeID { get; set; }
        public virtual POIType Type { get; set; }
        [Required]
        public DbGeography Coordinates { get; set; }
        public string Address { get; set; }
    }

    public class POIType
    {
        public POIType()
        {
            POIs = new HashSet<PointOfInterest>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }
        [Required]
        public string Name { get; set; }

        public virtual ICollection<PointOfInterest> POIs { get; set; }
    }
}