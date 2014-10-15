﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Casket.Connections;
using StarryEyes.Casket.Cruds.Scaffolding;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public abstract class ActivityCrudBase : CrudBase<DatabaseActivity>
    {
        private readonly string _tableName;

        protected ActivityCrudBase(string tableName)
            : base(tableName, ResolutionMode.Ignore)
        {
            _tableName = tableName;
        }

        internal override async Task InitializeAsync(IDatabaseConnectionDescriptor descriptor)
        {
            await base.InitializeAsync(descriptor);
            await this.CreateIndexAsync(_tableName + "_SID", "StatusId", false);
            await this.CreateIndexAsync(_tableName + "_UID", "UserId", false);
        }

        public Task<IEnumerable<long>> GetUsersAsync(long statusId)
        {
            return Descriptor.QueryAsync<long>(
                "select UserId from " + TableName + " where StatusId = @Id;",
                new { Id = statusId });
        }

        public async Task<Dictionary<long, IEnumerable<long>>> GetUsersDictionaryAsync(
            IEnumerable<long> statusIds)
        {
            return (await Descriptor.QueryAsync<DatabaseActivity>(
                this.CreateSql("StatusId IN @Ids"), new { Ids = statusIds.ToArray() }))
                .GroupBy(e => e.StatusId)
                .ToDictionary(e => e.Key, e => e.Select(d => d.UserId));
        }

        public async Task DeleteNotExistsAsync(string statusTableName)
        {
            await Descriptor.ExecuteAsync(
                string.Format("DELETE FROM {0} WHERE NOT EXISTS (SELECT Id FROM {1} WHERE {1}.Id = {0}.StatusId);",
                    this.TableName, statusTableName));
        }

        public async Task InsertAsync(long statusId, long userId)
        {
            await base.InsertAsync(new DatabaseActivity
            {
                StatusId = statusId,
                UserId = userId
            });
        }

        public async Task DeleteAsync(long statusId, long userId)
        {
            await Descriptor.ExecuteAsync(
                string.Format("delete from {0} where StatusId = @Sid and UserId = @Uid;", this.TableName),
                new { Sid = statusId, Uid = userId });
        }

        public async Task InsertAllAsync(IEnumerable<Tuple<long, long>> items)
        {
            var inserters = items.Select(i => Tuple.Create(
                TableInserter,
                (object)new DatabaseActivity
                {
                    StatusId = i.Item1,
                    UserId = i.Item2
                }));
            await Descriptor.ExecuteAllAsync(inserters);
        }

        public async Task DeleteAllAsync(IEnumerable<Tuple<long, long>> items)
        {
            var deleters = items.Select(i => Tuple.Create(
                "delete from " + this.TableName + " where StatusId = @Sid and UserId = @Uid;",
                (object)new { Sid = i.Item1, Uid = i.Item2 }));
            await Descriptor.ExecuteAllAsync(deleters);
        }
    }
}
