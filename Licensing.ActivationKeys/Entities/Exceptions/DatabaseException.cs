namespace SNBS.Licensing.Entities.Exceptions
{
	/// <summary>
	///	This exception is thrown when the licenses database is inaccessible.
	/// </summary>
	/// <remarks>
	///	Possible causes:
	/// 
	///	<list type="bullet">
	///	<item>Invalid connection string</item>
	///	<item>Modifications to database structure (never make them!)</item>
	///	<item>Not enough permissions</item>
	///	<item>And so on</item>
	///	</list>
	/// </remarks>
	[Serializable]
	public class DatabaseException : Exception
	{
		private static string message = "An error occurred (see the inner exception) when " +
			"querying the licenses database. This can be caused by invalid connection string, " +
			"modifications to the database structure (they're a bad idea!) etc.";

		/// <summary>
		/// This constructor creates an instance of <see cref="DatabaseException"/>,
		/// setting the inner exception.
		/// </summary>
		/// <param name="inner">The inner exception.</param>
		public DatabaseException(Exception inner) : base(message, inner) { }
	}
}
