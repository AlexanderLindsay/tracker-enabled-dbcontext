﻿using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using TrackerEnabledDbContext;

namespace SampleLogMaker.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection")
        {
        }
    }

    public class MyDbContext : TrackerContext
    {
        public MyDbContext() : base("DefaultConnection",true) 
        {
        }

        public DbSet<Blog> Blogs { get; set; }
    }
}