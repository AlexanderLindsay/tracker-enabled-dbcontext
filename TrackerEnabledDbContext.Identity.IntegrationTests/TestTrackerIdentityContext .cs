﻿using Microsoft.AspNet.Identity.EntityFramework;
using System.Data.Entity;
using TrackerEnabledDbContext;
using TrackerEnabledDbContext.Common.Testing.Models;

namespace TrackerEnabledDbContext.Identity.IntegrationTests
{
    public class TestTrackerIdentityContext : TrackerIdentityContext<IdentityUser>
    {
        public TestTrackerIdentityContext()
            : base("DefaultTestConnection")
        {
        }

        public DbSet<NormalModel> NormalModels { get; set; }
        public DbSet<ParentModel> ParentModels { get; set; }
        public DbSet<ChildModel> Children { get; set; }
        public DbSet<ModelWithCompositeKey> ModelsWithCompositeKey { get; set; }
        public DbSet<ModelWithConventionalKey> ModelsWithConventionalKey { get; set; }
    }
}
