using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using MySql.Data.MySqlClient;

namespace DataDevelop.Data.MySql
{
	internal sealed class MySqlDatabase : Database, IDisposable
	{
		private string name;
		private MySqlConnection connection;

		public MySqlDatabase(string name, string connectionString)
		{
			this.name = name;
			this.connection = new MySqlConnection(connectionString);
		}

		public override string Name
		{
			get { return this.name; }
		}

		public override bool SupportStoredProcedures
		{
			get { return true; }
		}

		public override string ParameterPrefix
		{
			get { return "?"; }
		}

		public override string QuotePrefix
		{
			get { return "`"; }
		}

		public override string QuoteSuffix
		{
			get { return "`"; }
		}

		public override DbProvider Provider
		{
			get { return new MySqlProvider(); }
		}

		public override string ConnectionString
		{
			get { return this.connection.ConnectionString; }
		}

		internal MySqlConnection Connection
		{
			get { return this.connection; }
		}

		public override int ExecuteNonQuery(string commandText)
		{
			MySqlCommand command = this.connection.CreateCommand();
			command.CommandText = commandText;
			return command.ExecuteNonQuery();
		}

		public override int ExecuteNonQuery(string commandText, System.Data.Common.DbTransaction transaction)
		{
			int rows = 0;
			try {
				this.Connect();
				MySqlCommand command = this.connection.CreateCommand();
				command.Transaction = (MySqlTransaction)transaction;
				command.CommandText = commandText;
				rows = command.ExecuteNonQuery();
			} finally {
				Disconnect();
			}
			return rows;
		}

		public override DataTable ExecuteTable(string commandText)
		{
			DataTable data = new DataTable();
			using (MySqlDataAdapter adapter = new MySqlDataAdapter(commandText, this.connection)) {
				adapter.Fill(data);
			}
			////using (MySqlCommand command = connection.CreateCommand()) {
			////    command.CommandText = commandText;
			////    using (MySqlDataReader reader = command.ExecuteReader()) {
			////        try {
			////            data.Load(reader);
			////        } catch (ConstraintException) {
			////        }
			////    }
			////}
			return data;
		}

		public override System.Data.Common.DbDataAdapter CreateAdapter(Table table, TableFilter filter)
		{
			MySqlDataAdapter adapter = new MySqlDataAdapter(table.GetBaseSelectCommandText(filter), this.connection);
			MySqlCommandBuilder builder = new MySqlCommandBuilder(adapter);
			////builder.ReturnGeneratedIdentifiers = true;
			try {
				adapter.InsertCommand = builder.GetInsertCommand();
				adapter.UpdateCommand = builder.GetUpdateCommand();
				adapter.DeleteCommand = builder.GetDeleteCommand();
			} catch {
			}
			return adapter;
		}

		public override System.Data.Common.DbCommand CreateCommand()
		{
			return this.connection.CreateCommand();
		}

		public override System.Data.Common.DbTransaction BeginTransaction()
		{
			return this.connection.BeginTransaction();
		}

		public void Dispose()
		{
			if (this.connection != null) {
				this.connection.Dispose();
				GC.SuppressFinalize(this);
			}
		}

		public override void ChangeConnectionString(string newConnectionString)
		{
			if (this.IsConnected) {
				throw new InvalidOperationException("Database must be disconnected in order to change the ConnectionString");
			} else {
				this.connection.ConnectionString = newConnectionString;
			}
		}

		protected override void DoConnect()
		{
			this.connection.Open();
		}

		protected override void DoDisconnect()
		{
			this.connection.Close();
		}

		protected override void PopulateTables(DbObjectCollection<Table> tablesCollection)
		{
			DataTable tables = this.Connection.GetSchema("Tables", new string[] { null, this.connection.Database });
			foreach (DataRow row in tables.Rows) {
				Table table = new MySqlTable(this);
				table.Name = row["TABLE_NAME"].ToString();
				tablesCollection.Add(table);
			}

			DataTable views = this.Connection.GetSchema("Views", new string[] { null, this.connection.Database });
			foreach (DataRow row in views.Rows) {
				MySqlTable table = new MySqlTable(this);
				table.Name = row["TABLE_NAME"].ToString();
				table.SetView(true);
				tablesCollection.Add(table);
			}
		}

		protected override void PopulateStoredProcedures(DbObjectCollection<StoredProcedure> storedProceduresCollection)
		{
			DataTable procedures = this.Connection.GetSchema("Procedures", new string[] { null, this.connection.Database });
			foreach (DataRow row in procedures.Rows) {
				MySqlStoredProcedure sp = new MySqlStoredProcedure(this, row);
				storedProceduresCollection.Add(sp);
			}
		}
	}
}
