﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atl.Repository.Standard.ApplicationContext;
using Atl.Repository.Standard.ApplicationContext.Contracts;
using Atl.Repository.Standard.DatabaseContents.Implementations;
using Atl.Repository.Standard.DepdendencyInjection.Contracts;
using Atl.Repository.Standard.DomainInjection.Contracts;
using Atl.Repository.Standard.Domains.Contracts;
using Atl.Repository.Standard.Repositories.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Internal;

namespace Atl.Repository.Standard.Repositories.Implementations
{
	public class Repository<TKey> : IGenericRepository<TKey>
	{
		private readonly IDomainContextFactory<DatabaseContext> _contextLocator;
		private readonly ISystemClock _clock;
		private readonly IEnumerable<IApplicationContext<TKey>> _applicationContexts;
		private readonly IKeyGenerator<TKey> _keyGenerator;

		/// <summary>
		/// Initializes a new instance of the <see cref="Repository{T}" /> class.
		/// </summary>
		/// <param name="keyGenerator">The key generator.</param>
		/// <param name="contextLocator">The context locator.</param>
		/// <param name="clock">The clock.</param>
		/// <param name="applicationContexts">The application contexts.</param>
		public Repository(IKeyGenerator<TKey> keyGenerator,
			IDomainContextFactory<DatabaseContext> contextLocator,
			ISystemClock clock,
			IEnumerable<IApplicationContext<TKey>> applicationContexts)
		{
			_keyGenerator = keyGenerator;
			_contextLocator = contextLocator;
			_clock = clock;
			_applicationContexts = applicationContexts;
		}

		#region Insert Mtethods
		/// <summary>
		/// Adds the specified object.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// <returns></returns>
		public virtual TDomain Add<TDomain>(TDomain obj) where TDomain : class, IDomain<TKey>
		{
			obj.Id = _keyGenerator.DoesRequireNewKey(obj.Id) ? _keyGenerator.Generate(obj) : obj.Id;
			obj.UpdatedAt = obj.CreatedAt = _clock.UtcNow.DateTime;
			foreach (var applicationContext in _applicationContexts ?? Enumerable.Empty< IApplicationContext<TKey>> ())
			{
				obj = (TDomain)applicationContext.ApplyContext(obj);
			}
			var context = _contextLocator.CreateDbContext();
			obj = context.Set<TDomain>().Add(obj).Entity;
			context.SaveChanges();
			return obj;
		}

		/// <summary>
		/// Updates the specified object.
		/// </summary>
		/// <typeparam name="TKey">The type of the key.</typeparam>
		/// <typeparam name="TDomain">The type of the domain.</typeparam>
		/// <param name="obj">The object.</param>
		/// <returns></returns>
		/// <exception cref="System.NotImplementedException"></exception>
		public virtual TDomain Update<TDomain>(TDomain obj) where TDomain : class, IDomain<TKey>
		{
			var context = _contextLocator.CreateDbContext();
			obj.UpdatedAt = _clock.UtcNow.DateTime;
			foreach (var applicationContext in _applicationContexts ?? Enumerable.Empty<IApplicationContext<TKey>>())
			{
				obj = (TDomain)applicationContext.ApplyContext(obj);
			}
			if (context.Entry(obj).State == EntityState.Detached)
			{
				context.Set<TDomain>().Attach(obj);
				context.Entry(obj).State = EntityState.Modified;
			}
			context.SaveChanges();
			return obj;
		}
		#endregion

		#region Get Methods
		/// <summary>
		/// Gets the by identifier.
		/// </summary>
		/// <typeparam name="TDomain">The type of the domain.</typeparam>
		/// <param name="id">The identifier.</param>
		/// <returns></returns>
		/// <exception cref="System.NotImplementedException"></exception>
		public virtual TDomain GetById<TDomain>(TKey id) where TDomain : class, IDomain<TKey>
		{
			return _contextLocator.CreateDbContext(new[] { "" }).Set<TDomain>().AsNoTracking()
				.FirstOrDefault(_keyGenerator.Equal<TDomain>(x => x.Id, id).Compile());
		}

		/// <summary>
		/// Gets all.
		/// </summary>
		/// <typeparam name="TDomain">The type of the domain.</typeparam>
		/// <returns></returns>
		public virtual IQueryable<TDomain> GetAll<TDomain>() where TDomain : class, IDomain<TKey>
		{
			return _contextLocator.CreateDbContext(new[] { "" }).Set<TDomain>().AsQueryable();
		}
		#endregion

		#region Delete Methods
		/// <summary>
		/// Deletes the specified identifier.
		/// </summary>
		/// <typeparam name="TKey">The type of the key.</typeparam>
		/// <typeparam name="TDomain">The type of the domain.</typeparam>
		/// <param name="id">The identifier.</param>
		/// <exception cref="System.NotImplementedException"></exception>
		public virtual TDomain Delete<TDomain>(TKey id) where TDomain : class, IDomain<TKey>
		{
			var context = _contextLocator.CreateDbContext();
			var obj = context.Set<TDomain>().FirstOrDefault(_keyGenerator.Equal<TDomain>(x => x.Id, id).Compile());
			return obj == null ? null : context.Set<TDomain>().Remove(obj).Entity;
		}

		/// <summary>
		/// Deletes the specified ids.
		/// </summary>
		/// <typeparam name="TDomain">The type of the domain.</typeparam>
		/// <param name="ids">The ids.</param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public virtual void DeleteRange<TDomain>(IEnumerable<TKey> ids) where TDomain : class, IDomain<TKey>
		{
			if (ids is null)
				return;
			var context = _contextLocator.CreateDbContext();
			var objs = context.Set<TDomain>().Where(x => ids.Contains(x.Id));
			context.Set<TDomain>().RemoveRange(objs);
		}

		/// <summary>
		/// Deletes the specified object.
		/// </summary>
		/// <typeparam name="TKey">The type of the key.</typeparam>
		/// <typeparam name="TDomain">The type of the domain.</typeparam>
		/// <param name="obj">The object.</param>
		/// <exception cref="System.NotImplementedException"></exception>
		public virtual TDomain Delete<TDomain>(TDomain obj) where TDomain : class, IDomain<TKey>
		{
			if (obj is null)
				return null;
			//check whether object is attached or not
			var context = _contextLocator.CreateDbContext();
			if (context.Entry(obj).State == EntityState.Detached)
			{
				context.Set<TDomain>().Attach(obj);
			}
			obj = context.Set<TDomain>().Remove(obj).Entity;
			context.SaveChanges();
			return obj;
		}

		/// <summary>
		/// Deletes the specified objects.
		/// </summary>
		/// <typeparam name="TDomain">The type of the domain.</typeparam>
		/// <param name="objects">The objects.</param>
		/// <returns></returns>
		public virtual void DeleteRange<TDomain>(IEnumerable<TDomain> objects) where TDomain : class, IDomain<TKey>
		{
			if (objects is null)
				return;
			var objectList = objects.ToList();

			var context = _contextLocator.CreateDbContext();
			foreach (var domain in objectList)
			{
				if (context.Entry(domain).State == EntityState.Detached)
				{
					context.Set<TDomain>().Attach(domain);
				}
			}
			context.Set<TDomain>().RemoveRange(objectList);
			context.SaveChanges();
		}

		#endregion
	}
}