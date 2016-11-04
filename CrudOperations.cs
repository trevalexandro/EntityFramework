using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RSI.Internal.TrainingTracker.Data
{
    public static class CrudOperations<TDatabaseContext> where TDatabaseContext : DbContext
    {
        /// <summary>
        /// Retrieves records from the database
        /// </summary>
        /// <typeparam name="TObjectType">The type of object to retrieve, must be a type of data model</typeparam>
        /// <param name="query">Filters the database to only records where the expression equals true, if null all records of the set will be returned</param>
        /// <param name="navigationProperties">Specifies which navigation properties need to be loaded by using the Include extension method,
        /// navigation properties that are not specified will be null</param>
        /// <param name="connectionStr">The connection string that will open a connection to the underlying database, if null a default connection string should
        /// be passed in the DbContext constructor</param>
        /// <returns>A collection of records of TObjectType</returns>
        public static IEnumerable<TObjectType> Get<TObjectType>(Func<TObjectType, bool> query = null, Func<IQueryable<TObjectType>, IQueryable<TObjectType>> navigationProperties = null, string connectionStr = null) where TObjectType : class
        {
            //Get an instance of DbContext
            using (var db = (DbContext)Activator.CreateInstance(typeof(TDatabaseContext), new object[] { connectionStr }))
            {
                //Get the DbSet of TObjectType
                var set = db.Set<TObjectType>().AsQueryable();
                //If there are any navigation properties that need to be loaded, load them
                if (navigationProperties != null)
                {
                    set = navigationProperties.Invoke(set);
                }
                return query == null ? set.ToList() : set.Where(query).ToList();
            }
        }

        /// <summary>
        /// Adds an object with a generic type to the database
        /// </summary>
        /// <typeparam name="TObjectType">The type of object being added to the database</typeparam>
        /// <param name="obj">The object being added, should be of type TObjectType</param>
        /// <param name="anonymousObjects">Overrides default behavior of Entity Framework that inserts new records for each instantiated navigation property,
        /// should be an array of anonymous objects each consisting of the navigation property to override and the desired EntityState</param>
        /// <param name="connectionStr">The connection string that will open a connection to the underlying database, if null a default connection string should
        /// be passed in the DbContext constructor</param>
        /// <returns>The object that was passed in to be added, the Id property will be set when it returns</returns>
        public static TObjectType Add<TObjectType>(TObjectType obj, Func<TObjectType, object[]> anonymousObjects = null, string connectionStr = null) where TObjectType : class
        {
            //Get an instance of DbContext
            using (var db = (DbContext)Activator.CreateInstance(typeof(TDatabaseContext), new object[] { connectionStr }))
            {
                if (anonymousObjects != null)
                {
                    //Get all the navigation properties for the type object being added to the database
                    var navigationProperties = ((IObjectContextAdapter)db).ObjectContext.CreateObjectSet<TObjectType>().EntitySet.ElementType.NavigationProperties;
                    //Iterate through all the objects passed in the anonymousObjects array
                    foreach (var anonymousObject in anonymousObjects.Invoke(obj))
                    {
                        //Get all the properties from the current anonymousObject
                        var anonymousObjectProperties = anonymousObject.GetType().GetProperties();
                        //Get the entity state property from the current anonymousObject
                        var state = anonymousObjectProperties.FirstOrDefault(p => p.PropertyType == typeof(EntityState));
                        //Get the navigation property from anonymousObject
                        var propertyFound = anonymousObjectProperties.FirstOrDefault(p => p.Name == navigationProperties.FirstOrDefault(np => np.Name == p.Name).Name);
                        if (propertyFound != null && state != null)
                        {
                            //If both an entity state and navigation property were found on anonymousObject, 
                            //attach propertyFound to DbContext and set EntityState using state
                            db.Entry(propertyFound.GetValue(anonymousObject)).State = (EntityState)state.GetValue(anonymousObject);
                        }
                    }
                }
                //Add obj to the database
                db.Set<TObjectType>().Add(obj);
                db.SaveChanges();
            }
            return obj;
        }

        /// <summary>
        /// Updates an existing generic type in the database
        /// </summary>
        /// <typeparam name="TObjectType">The type of object in the database being updated</typeparam>
        /// <param name="obj">The object being updated, should be of type TObjectType</param>
        /// <param name="connectionStr">The connection string that will open a connection to the underlying database, if null a default connection string should
        /// be passed in the DbContext constructor</param>
        public static void Update<TObjectType>(TObjectType obj, string connectionStr = null) where TObjectType : class
        {
            //Get an instance of DbContext
            using (var db = (DbContext)Activator.CreateInstance(typeof(TDatabaseContext), new object[] { connectionStr }))
            {
                //Sets the state of the entity being updated to modified so the database will know to perform an update operation
                db.Entry(obj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }
    }
}
