﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using TrackerEnabledDbContext.Common.Extensions;
using TrackerEnabledDbContext.Common.Interfaces;
using TrackerEnabledDbContext.Common.Models;

namespace TrackerEnabledDbContext.Common.Testing.Extensions
{
    public static class AuditAssertExtensions
    {
        public static T AssertAuditForAddition<T>(this T entity, ITrackerContext db, object entityId,
            string userName = null, params Expression<Func<T, object>>[] propertyExpressions)
        {
            IEnumerable<AuditLog> logs = db.GetLogs<T>(entityId)
                .AssertCountIsNotZero("log count is zero");

            AuditLog lastLog = logs.Last(x => x.EventType == EventType.Added && x.UserName == userName)
                .AssertIsNotNull("log not found");

            lastLog.LogDetails
                .AssertCountIsNotZero("no log details found")
                .AssertCount(propertyExpressions.Count());

            foreach (var expression in propertyExpressions)
            {
                var keyValuePair = entity.GetKeyValuePair(expression);

                lastLog.LogDetails.AssertAny(x => x.NewValue == keyValuePair.Value
                                                  && x.PropertyName == keyValuePair.Key);
            }

            return entity;
        }

        public static T AssertAuditForDeletion<T>(this T entity, ITrackerContext db, object entityId,
            string userName = null, params Expression<Func<T, object>>[] oldValueProperties)
        {
            IEnumerable<AuditLog> logs = db.GetLogs<T>(entityId)
                .AssertCountIsNotZero("log count is zero");

            AuditLog lastLog = logs.Last(x => x.EventType == EventType.Deleted && x.UserName == userName)
                .AssertIsNotNull("log not found");

            lastLog.LogDetails
                .AssertCountIsNotZero("no log details found")
                .AssertCount(oldValueProperties.Count());

            foreach (var property in oldValueProperties)
            {
                var keyValuePair = entity.GetKeyValuePair(property);
                lastLog.LogDetails.AssertAny(x => x.OriginalValue == keyValuePair.Value
                                                  && x.PropertyName == keyValuePair.Key);
            }

            return entity;
        }

        public static T AssertAuditForSoftDeletion<T>(this T entity, ITrackerContext db, object entityId,
            string userName = null, params AuditLogDetail[] logdetails)
        {
            var logs = db.GetLogs<T>(entityId)
                .Where(x => x.EventType == EventType.SoftDeleted && x.UserName == userName);

            logs.AssertCountIsNotZero(
                $"no logs found for {typeof (T).Name} with id {entityId} & username {userName ?? "null"}");

            var lastLog = logs.LastOrDefault().AssertIsNotNull();

            lastLog.LogDetails
                .AssertCountIsNotZero("no log details found")
                .AssertCount(logdetails.Count());

            foreach (var auditLogDetail in logdetails)
            {
                logdetails.AssertAny(x => x.OriginalValue == auditLogDetail.OriginalValue
                                          && x.NewValue == auditLogDetail.NewValue
                                          && x.PropertyName == auditLogDetail.PropertyName, 
                                          $"cound not find log detail with original value: {auditLogDetail.OriginalValue}, " +
                                          $"new value: {auditLogDetail.NewValue} " +
                                          $"and propertyname: {auditLogDetail.PropertyName}");
            }

            return entity;
        }

        public static T AssertAuditForModification<T>(this T entity, ITrackerContext db, object entityId,
            object userName = null, params AuditLogDetail[] logdetails)
        {
            IEnumerable<AuditLog> logs = db.GetLogs<T>(entityId).ToList();
            logs.AssertCountIsNotZero("log count is zero");

            AuditLog lastLog = logs.Last(
                x => x.EventType == EventType.Modified && x.UserName == userName?.ToString())
                .AssertIsNotNull("log not found");

            lastLog.LogDetails
                .AssertCountIsNotZero("no log details found")
                .AssertCount(logdetails.Count());

            foreach (AuditLogDetail logdetail in logdetails)
            {
                lastLog.LogDetails.AssertAny(x => x.OriginalValue == logdetail.OriginalValue
                                                  && x.PropertyName == logdetail.PropertyName
                                                  && x.NewValue == logdetail.NewValue,
                    "could not find an expected auditlog detail");
            }

            return entity;
        }

        public static T AssertNoLogs<T>(this T entity, ITrackerContext db, object entityId)
        {
            var logs = db.GetLogs<T>(entityId);
            logs.AssertCount(0, "Logs found when logs were not expected");

            return entity;
        }
    }
}