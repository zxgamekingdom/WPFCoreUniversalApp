﻿using DesktopUniversalCustomControl.CustomView.MsgDlg;
using DesktopUniversalFrame.Common.Buffer;
using DesktopUniversalFrame.Common.MappingAttribute;
using DesktopUniversalFrame.Model;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace DesktopUniversalFrame.Entity
{
    /// <summary>
    /// ORM
    /// </summary>
    public class ORMHelper
    {
        private static string connectionStr = ConfigurationManager.ConnectionStrings["connectionStr"].ConnectionString;

        //查询所有
        public static List<T> QueryData<T>()
        {
            Type type = typeof(T);
            string commandText = SqlBuilder<T>.GetSql(SqlOperationType.SelectAll);
            T t = Activator.CreateInstance<T>();
            var param = new MySqlParameter[] { };
            List<T> list = new List<T>();

            return ExecutedSql(commandText, param, cmd =>
            {
                var reader = cmd.ExecuteReader();               
                while (reader.Read())
                {
                    t = Activator.CreateInstance<T>();
                    foreach (var prop in type.GetProperties().ExcepteIgnoreProperty())
                    {
                        var ss = reader[prop.GetAttributeMappingName()];
                        prop.SetValue(t, reader[prop.GetAttributeMappingName()] is DBNull ? null : reader[prop.GetAttributeMappingName()]);
                    }
                    list.Add(t);                
                }
                return list;
            });
        }

        //数据查询
        public static T QueryData<T>(string id)
        {
            Type type = typeof(T);
            string commandText = SqlBuilder<T>.GetSql(SqlOperationType.Select);
            var parameterList = new MySqlParameter[] { new MySqlParameter("@id", id) };
            T t = Activator.CreateInstance<T>();

            return ExecutedSql<T>(commandText, parameterList, cmd =>
            {
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    foreach (var prop in type.GetProperties().ExceptKey().ExcepteIgnoreProperty())
                    {
                        prop.SetValue(t, reader[prop.GetAttributeMappingName()] is DBNull ? null : reader[prop.GetAttributeMappingName()]);
                    }

                    return t;
                }
                else
                {
                    return default(T);
                }
            });
        }

        //数据插入
        public static bool InsertData<T>(T t) where T : BaseModel
        {
            Type type = typeof(T);
            //设定主键值Guid
            //type.GetProperties().GetKeyInfo().SetValue(t, Guid.NewGuid().ToString());
            //type.GetProperties().GetKeyInfo().SetValue(t, Guid.NewGuid().ToString());
            string commandText = SqlBuilder<T>.GetSql(SqlOperationType.Insert);
            var parameterList = type.GetProperties().ExceptKey().ExcepteIgnoreProperty().Select(p => new MySqlParameter($"@{p.Name}", p.GetValue(t) ?? DBNull.Value));

            return ExecutedSql<bool>(commandText, parameterList.ToArray(), cmd =>
            {
                int result = cmd.ExecuteNonQuery();
                return result == 1;
            });
        }

        //数据更新
        public static bool Update<T>(T t) where T : BaseModel
        {
            Type type = typeof(T);
            string commandText = SqlBuilder<T>.GetSql(SqlOperationType.Update);
            var parameterList = type.GetProperties().Select(p => new MySqlParameter($"@{p.Name}", p.GetValue(t) ?? DBNull.Value));

            return ExecutedSql<bool>(commandText, parameterList.ToArray(), cmd =>
            {
                int result = cmd.ExecuteNonQuery();
                return result == 1;
            });
        }

        //数据删除
        public static bool Delete<T>(T t) where T : BaseModel
        {
            Type type = typeof(T);
            string commandText = SqlBuilder<T>.GetSql(SqlOperationType.Delete);
            var parameterList = new MySqlParameter[] { new MySqlParameter($"@id", type.GetProperties().GetKeyInfo().GetValue(t))};

            return ExecutedSql<bool>(commandText, parameterList.ToArray(), cmd =>
            {
                int result = cmd.ExecuteNonQuery();
                return result == 1;
            });
        }

        /// <summary>
        /// Sql语句操作
        /// </summary>
        private static T ExecutedSql<T>(string commandText, MySqlParameter[] parameters, Func<MySqlCommand,T> func)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(connectionStr))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(commandText, con))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        return func.Invoke(cmd);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageDialog.Show(ex.Message);
                return default(T);
            }
        }
    }
}
