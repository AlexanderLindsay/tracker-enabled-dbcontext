﻿using System;
using System.Globalization;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using TrackerEnabledDbContext.Common.Testing.Models;
using TrackerEnabledDbContext.Common.Testing.Extensions;
using System.Threading.Tasks;
using System.Collections.Generic;
using TrackerEnabledDbContext.Common.Testing;
using TrackerEnabledDbContext.Common.Models;


namespace TrackerEnabledDbContext.IntegrationTests
{
    [TestClass]
    public class TrackerContextIntegrationTests : PersistanceTests<TestTrackerContext>
    {
        [TestMethod]
        public void Can_save_model()
        {
            var model = ObjectFactory<NormalModel>.Create();
            db.NormalModels.Add(model);
            db.SaveChanges();
            model.Id.AssertIsNotZero();
        }

        [TestMethod]
        public void Can_save_when_entity_state_changed()
        {
            var model = ObjectFactory<NormalModel>.Create();
            db.Entry(model).State = System.Data.Entity.EntityState.Added;
            db.SaveChanges();
            model.Id.AssertIsNotZero();
        }

        [TestMethod]
        public async Task Can_save_async()
        {
            var model = ObjectFactory<NormalModel>.Create();
            db.Entry(model).State = System.Data.Entity.EntityState.Added;
            await db.SaveChangesAsync();
            model.Id.AssertIsNotZero();
        }

        [TestMethod]
        public void Can_save_child_to_parent()
        {
            var child = new ChildModel();
            var parent = new ParentModel();
            child.Parent = parent;

            db.Children.Add(child);

            db.SaveChanges();

            child.Id.AssertIsNotZero();
            parent.Id.AssertIsNotZero();
        }

        [TestMethod]
        public void Can_save_child_to_parent_when_entity_state_changed()
        {
            var child = new ChildModel();
            var parent = new ParentModel();
            child.Parent = parent;

            db.Entry(child).State = System.Data.Entity.EntityState.Added;

            db.SaveChanges();

            child.Id.AssertIsNotZero();
            parent.Id.AssertIsNotZero();
        }

        [TestMethod]
        public void Can_track_addition_when_username_provided()
        {
            var randomText = RandomText;
            var userName = RandomText;

            var normalModel = ObjectFactory<NormalModel>.Create();
            normalModel.Description = randomText;
            db.NormalModels.Add(normalModel);
            db.SaveChanges(userName);

            normalModel.AssertAuditForAddition(db, normalModel.Id, userName,
                new KeyValuePair<string, string>("Description", randomText),
                new KeyValuePair<string, string>("Id", normalModel.Id.ToString())
                );
        }

        [TestMethod]
        public void Can_track_addition_when_usermane_not_provided()
        {
            var randomText = RandomText;

            var normalModel = ObjectFactory<NormalModel>.Create();
            normalModel.Description = randomText;
            db.NormalModels.Add(normalModel);
            db.SaveChanges();

            normalModel.AssertAuditForAddition(db, normalModel.Id, null,
                new KeyValuePair<string, string>("Description", randomText),
                new KeyValuePair<string, string>("Id", normalModel.Id.ToString())
                );
        }

        [TestMethod]
        public void Can_track_addition_when_state_changed_directly()
        {
            var randomText = RandomText;
            var userName = RandomText;

            var model = ObjectFactory<NormalModel>.Create();
            model.Description = randomText;
            db.Entry(model).State = System.Data.Entity.EntityState.Added;
            db.SaveChanges(userName);

            model.AssertAuditForAddition(db, model.Id, userName,
                new KeyValuePair<string, string>("Description", randomText),
                new KeyValuePair<string, string>("Id", model.Id.ToString())
                );
        }

        [TestMethod]
        public void Can_track_deletion()
        {
            var description = RandomText;
            var userName = RandomText;

            //add
            var normalModel = ObjectFactory<NormalModel>.Create();
            normalModel.Description = description;
            db.NormalModels.Add(normalModel);
            db.SaveChanges(userName);


            //remove
            db.NormalModels.Remove(normalModel);
            db.SaveChanges(userName);

            normalModel.AssertAuditForDeletion(db, normalModel.Id, userName,
                new KeyValuePair<string, string>("Description", normalModel.Description),
                new KeyValuePair<string, string>("Id", normalModel.Id.ToString())
                );
        }

        [TestMethod]
        public void Can_track_deletion_when_state_changed()
        {
            var description = RandomText;

            //add
            var normalModel = ObjectFactory<NormalModel>.Create();
            normalModel.Description = description;
            db.NormalModels.Add(normalModel);
            db.SaveChanges();


            //remove
            db.Entry(normalModel).State = System.Data.Entity.EntityState.Deleted;
            db.SaveChanges();


            //assert
            normalModel.AssertAuditForDeletion(db, normalModel.Id, null,
                new KeyValuePair<string, string>("Description", normalModel.Description),
                new KeyValuePair<string, string>("Id", normalModel.Id.ToString())
                );
        }

        [TestMethod]
        public void Can_track_local_propery_change()
        {
            //add enity
            var oldDescription = RandomText;
            var newDescription = RandomText;
            var entity = new NormalModel { Description = oldDescription };
            db.Entry(entity).State = System.Data.Entity.EntityState.Added;
            db.SaveChanges();

            //modify entity
            entity.Description = newDescription;
            db.SaveChanges();

            var expectedLog = new List<AuditLogDetail> {
                new AuditLogDetail{
                    NewValue = newDescription,
                    OriginalValue = oldDescription,
                    ColumnName = "Description"
                }}.ToArray();


            //assert
            entity.AssertAuditForModification(db, entity.Id, null, expectedLog);
        }

        [TestMethod]
        public void Can_track_navigational_property_change()
        {
            //add enitties
            var parent1 = new ParentModel();
            var child = new ChildModel { Parent = parent1 };
            db.Children.Add(child);
            db.SaveChanges();

            child.Id.AssertIsNotZero(); //assert child saved
            parent1.Id.AssertIsNotZero(); //assert parent1 saved

            //save parent 2
            var parent2 = new ParentModel();
            db.ParentModels.Add(parent2);
            db.SaveChanges();

            parent2.Id.AssertIsNotZero(); //assert parent2 saved

            //change parent
            child.Parent = parent2;
            db.SaveChanges();

            var expectedLog = new List<AuditLogDetail> {
                new AuditLogDetail{
                    NewValue = parent2.Id.ToString(),
                    OriginalValue = parent1.Id.ToString(),
                    ColumnName = "ParentId"
                }}.ToArray();

            //assert change
            child.AssertAuditForModification(db, child.Id, null, expectedLog);
        }

        [TestMethod]
        public async Task Can_skip_tracking_of_property()
        {
            string username = RandomText;

            //add enitties
            var entity = new ModelWithSkipTracking { TrackedProperty = Guid.NewGuid(), UnTrackedProperty = RandomText };
            db.ModelsWithSkipTracking.Add(entity);
            await db.SaveChangesAsync(username, CancellationToken.None);

            //assert enity added
            entity.Id.AssertIsNotZero();

            //assert addtion
            entity.AssertAuditForAddition(db, entity.Id, username,
                new KeyValuePair<string, string>("TrackedProperty", entity.TrackedProperty.ToString()),
                new KeyValuePair<string, string>("Id", entity.Id.ToString(CultureInfo.InvariantCulture))
                );
        }

        [TestMethod]
        public void Can_track_composite_keys()
        {
            var key1 = RandomText;
            var key2 = RandomText;
            var userName = RandomText;
            var descr = RandomText;


            var entity = ObjectFactory<ModelWithCompositeKey>.Create();
            entity.Description = descr;
            entity.Key1 = key1;
            entity.Key2 = key2;

            db.ModelsWithCompositeKey.Add(entity);
            db.SaveChanges(userName);

            string expectedKey = string.Format("[{0},{1}]", key1, key2);

            entity.AssertAuditForAddition(db, expectedKey, userName,
                new KeyValuePair<string, string>("Description", descr),
                new KeyValuePair<string, string>("Key1", key1),
                new KeyValuePair<string, string>("Key2", key2)
                );
        }

        [TestMethod]
        public async Task Can_get_logs_by_table_name()
        {
            string descr = RandomText;
            var model = ObjectFactory<NormalModel>.Create();
            model.Description = descr;

            db.NormalModels.Add(model);
            await db.SaveChangesAsync(CancellationToken.None);
            model.Id.AssertIsNotZero();

            var logs = db.GetLogs("NormalModels", model.Id)
                .AssertCountIsNotZero("logs not found");

            var lastLog = logs.LastOrDefault().AssertIsNotNull("last log is null");

            var details = lastLog.LogDetails
                .AssertIsNotNull("log details is null")
                .AssertCountIsNotZero("no log details found");
        }

        [TestMethod]
        public async Task Can_get_logs_by_entity_type()
        {
            string descr = RandomText;
            var model = ObjectFactory<NormalModel>.Create();
            model.Description = descr;

            db.NormalModels.Add(model);
            await db.SaveChangesAsync(CancellationToken.None);
            model.Id.AssertIsNotZero();

            var logs = db.GetLogs<NormalModel>(model.Id)
                .AssertCountIsNotZero("logs not found");

            var lastLog = logs.LastOrDefault().AssertIsNotNull("last log is null");

            var details = lastLog.LogDetails
                .AssertIsNotNull("log details is null")
                .AssertCountIsNotZero("no log details found");
        }

        [TestMethod]
        public async Task Can_get_all_logs()
        {
            string descr = RandomText;
            var model = ObjectFactory<NormalModel>.Create();
            model.Description = descr;

            db.NormalModels.Add(model);
            await db.SaveChangesAsync(RandomText);
            model.Id.AssertIsNotZero();

            var logs = db.GetLogs("NormalModels")
                .AssertCountIsNotZero("logs not found");

            var lastLog = logs.LastOrDefault().AssertIsNotNull("last log is null");

            var details = lastLog.LogDetails
                .AssertIsNotNull("log details is null")
                .AssertCountIsNotZero("no log details found");
        }
        
        [TestMethod]
        public async Task Can_save_changes_with_userID()
        {
            int userId = RandomNumber;

            //add enity
            var oldDescription = RandomText;
            var newDescription = RandomText;
            var entity = new NormalModel { Description = oldDescription };
            db.Entry(entity).State = System.Data.Entity.EntityState.Added;
            db.SaveChanges();

            //modify entity
            entity.Description = newDescription;
            await db.SaveChangesAsync(userId);

            var expectedLog = new List<AuditLogDetail> {
                new AuditLogDetail{
                    NewValue = newDescription,
                    OriginalValue = oldDescription,
                    ColumnName = "Description"
                }}.ToArray();


            //assert
            entity.AssertAuditForModification(db, entity.Id, userId, expectedLog);
        }
    }
}
