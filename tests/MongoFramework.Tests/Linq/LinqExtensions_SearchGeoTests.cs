﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoFramework.Attributes;
using MongoFramework.Linq;

namespace MongoFramework.Tests.Linq
{
	[TestClass]
	public class LinqExtensions_SearchGeoTests : TestBase
	{
		public class SearchGeoModel
		{
			public string Id { get; set; }
			public string Description { get; set; }
			[Index(IndexType.Geo2dSphere)]
			public GeoJsonPoint<GeoJson2DGeographicCoordinates> PrimaryCoordinates { get; set; }
			[Index(IndexType.Geo2dSphere)]
			public GeoJsonPoint<GeoJson2DGeographicCoordinates> SecondaryCoordinates { get; set; }

			[ExtraElements]
			public IDictionary<string, object> ExtraElements { get; set; }
			public double CustomDistanceField { get; set; }
		}

		[TestMethod]
		public void SearchGeoNear()
		{
			var connection = TestConfiguration.GetConnection();
			var dbSet = new MongoDbSet<SearchGeoModel>();
			dbSet.SetConnection(connection);

			dbSet.AddRange(new SearchGeoModel[]
			{
				new SearchGeoModel { Description = "New York", PrimaryCoordinates = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
					new GeoJson2DGeographicCoordinates(-74.005974, 40.712776)
				) },
				new SearchGeoModel { Description = "Adelaide", PrimaryCoordinates = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
					new GeoJson2DGeographicCoordinates(138.600739, -34.928497)
				) },
				new SearchGeoModel { Description = "Perth", PrimaryCoordinates = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
					new GeoJson2DGeographicCoordinates(115.860458, -31.950527)
				) },
				new SearchGeoModel { Description = "Hobart", PrimaryCoordinates = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
					new GeoJson2DGeographicCoordinates(147.327194, -42.882137)
				) }
			});
			dbSet.SaveChanges();

			var results = dbSet.SearchGeoNear(e => e.PrimaryCoordinates, new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
				new GeoJson2DGeographicCoordinates(138, -30)
			)).ToArray();

			Assert.AreEqual(4, results.Count());
			Assert.AreEqual(138.600739, results[0].PrimaryCoordinates.Coordinates.Longitude);
			Assert.AreEqual(-34.928497, results[0].PrimaryCoordinates.Coordinates.Latitude);
			Assert.AreEqual(-74.005974, results[3].PrimaryCoordinates.Coordinates.Longitude);
			Assert.AreEqual(40.712776, results[3].PrimaryCoordinates.Coordinates.Latitude);

			Assert.IsTrue(results[0].ExtraElements.ContainsKey("Distance"));
		}

		[TestMethod]
		public void SearchGeoNearWithCustomDistanceField()
		{
			var connection = TestConfiguration.GetConnection();
			var dbSet = new MongoDbSet<SearchGeoModel>();
			dbSet.SetConnection(connection);

			dbSet.AddRange(new SearchGeoModel[]
			{
				new SearchGeoModel { Description = "New York", PrimaryCoordinates = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
					new GeoJson2DGeographicCoordinates(-74.005974, 40.712776)
				) },
				new SearchGeoModel { Description = "Adelaide", PrimaryCoordinates = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
					new GeoJson2DGeographicCoordinates(138.600739, -34.928497)
				) }
			});
			dbSet.SaveChanges();

			var results = dbSet.SearchGeoNear(e => e.PrimaryCoordinates, new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
				new GeoJson2DGeographicCoordinates(138, -30)
			), distanceResultField: e => e.CustomDistanceField).ToArray();

			Assert.AreNotEqual(0, results[0].CustomDistanceField);
			Assert.AreNotEqual(0, results[1].CustomDistanceField);
			Assert.IsTrue(results[0].CustomDistanceField < results[1].CustomDistanceField);

			Assert.IsNull(results[0].ExtraElements);
		}

		[TestMethod]
		public void SearchGeoNearWithMinMaxDistances()
		{
			var connection = TestConfiguration.GetConnection();
			var dbSet = new MongoDbSet<SearchGeoModel>();
			dbSet.SetConnection(connection);

			dbSet.AddRange(new SearchGeoModel[]
			{
				new SearchGeoModel { Description = "New York", PrimaryCoordinates = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
					new GeoJson2DGeographicCoordinates(-74.005974, 40.712776)
				) },
				new SearchGeoModel { Description = "Adelaide", PrimaryCoordinates = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
					new GeoJson2DGeographicCoordinates(138.600739, -34.928497)
				) },
				new SearchGeoModel { Description = "Perth", PrimaryCoordinates = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
					new GeoJson2DGeographicCoordinates(115.860458, -31.950527)
				) },
				new SearchGeoModel { Description = "Hobart", PrimaryCoordinates = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
					new GeoJson2DGeographicCoordinates(147.327194, -42.882137)
				) }
			});
			dbSet.SaveChanges();
			
			SearchGeoModel[] GetResults(double? maxDistance = null, double? minDistance = null)
			{
				return dbSet.SearchGeoNear(e => e.PrimaryCoordinates, new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
					new GeoJson2DGeographicCoordinates(138, -30)
				), distanceResultField: e => e.CustomDistanceField, maxDistance: maxDistance, minDistance: minDistance).ToArray();
			}

			var results = GetResults(maxDistance: 3000000);
			Assert.AreEqual(3, results.Count());
			Assert.IsTrue(results.Max(e => e.CustomDistanceField) < 3000000);

			results = GetResults(maxDistance: 600000);
			Assert.AreEqual(1, results.Count());
			Assert.IsTrue(results.Max(e => e.CustomDistanceField) < 600000);

			results = GetResults(maxDistance: 17000000);
			Assert.AreEqual(4, results.Count());

			results = GetResults(minDistance: 600000);
			Assert.AreEqual(3, results.Count());
			Assert.IsTrue(results.Min(e => e.CustomDistanceField) > 600000);

			results = GetResults(maxDistance: 3000000, minDistance: 600000);
			Assert.AreEqual(2, results.Count());
			Assert.IsTrue(results.Max(e => e.CustomDistanceField) < 3000000);
			Assert.IsTrue(results.Min(e => e.CustomDistanceField) > 600000);
		}

		[TestMethod]
		public void SearchGeoIntersects()
		{

		}
	}
}
