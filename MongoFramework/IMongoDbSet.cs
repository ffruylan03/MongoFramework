﻿using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MongoFramework
{
	public interface IMongoDbSet
	{
		void SetDatabase(IMongoDatabase database);
		void SaveChanges();
	}

	public interface IAsyncMongoDbSet : IMongoDbSet
	{
		Task SaveChangesAsync();
	}

	public interface IMongoDbSet<TEntity> : IMongoDbSet, IQueryable<TEntity>
	{
		void Add(TEntity entity);
		void AddRange(IEnumerable<TEntity> entities);
		void Update(TEntity entity);
		void UpdateRange(IEnumerable<TEntity> entities);
		void Remove(TEntity entity);
		void RemoveRange(IEnumerable<TEntity> entities);
	}
}
