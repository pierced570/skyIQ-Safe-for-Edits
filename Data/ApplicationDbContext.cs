using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkyIQ.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyIQ.Data
{
	public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options)
		{
		}
	
		public DbSet<Aircraft> Aircrafts { get; set; }
		public DbSet<TripSummaryDb> Trips { get; set; }
		public DbSet<EmailList> EmailLists { get; set;}
		public DbSet<CarryType> CarryTypes { get; set; }
	}

}
