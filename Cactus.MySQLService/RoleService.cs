﻿using Cactus.IService;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper.Common;
using Cactus.Common;

namespace Cactus.MySQLService
{
    public class RoleService : IRoleService
    {
        public bool Insert(Model.Sys.Role entity)
        {
            using (IDbConnection conn = SqlString.GetMySqlConnection())
            {
                int i = conn.Execute("INSERT INTO sys_role(RoleName,ActionIds)VALUES(@RoleName,@ActionIds)", entity);
                if (i > 0) { return true; } else { return false; }
            }
        }

        public bool InsertBatch(List<Model.Sys.Role> datas)
        {
            throw new NotImplementedException();
        }

        public void Update(Model.Sys.Role entity)
        {
            using (IDbConnection conn = SqlString.GetMySqlConnection())
            {
                conn.Execute("UPDATE sys_role SET RoleName=@RoleName,ActionIds=@ActionIds WHERE Role_Id =@Role_Id", entity);
            }
        }

        public void Delete(string ids)
        {
            using (IDbConnection conn = SqlString.GetMySqlConnection())
            {
                conn.Execute(string.Format("DELETE FROM sys_role WHERE Role_Id in ({0})", ids));
            }
        }

        public List<Model.Sys.Role> GetAll()
        {
            using (IDbConnection conn = SqlString.GetMySqlConnection())
            {
                return conn.Query<Model.Sys.Role>("select a.* from sys_role as a").ToList();
            }
        }

        public List<Model.Sys.Role> ToPagedList(int pageIndex, int pageSize, string keySelector,out int count)
        {
            /*
                * firstIndex:起始索引
                * pageSize:每页显示的数量
                * orderColumn:排序的字段名
                * sql:可以是简单的单表查询语句，也可以是复杂的多表联合查询语句
            */
            using (IDbConnection conn = SqlString.GetMySqlConnection())
            {
                string sql = "select a.* from sys_role as a ";
                string sql01 = "select count(*) from sys_role";
                count = conn.Query<int>(sql01).SingleOrDefault();

                string query = "SELECT * from (" + sql + ")as c ORDER BY " + keySelector+ " LIMIT " + (pageIndex - 1) * pageSize + "," + pageSize;
                return conn.Query< Model.Sys.Role>(query).ToList();
            }
        }

        public Model.Sys.Role Find(int id)
        {
            using (IDbConnection conn = SqlString.GetMySqlConnection())
            {
                string query = "select a.* from sys_role as a WHERE a.Role_Id = @id";
                return conn.Query<Model.Sys.Role>(query, new { id = id }).SingleOrDefault();
            }
        }

        public bool IsUseName(string rolename, int ignoreId)
        {
            using (IDbConnection conn = SqlString.GetMySqlConnection())
            {
                int i = conn.Query<int>("SELECT c.Role_Id FROM sys_role as c WHERE c.RoleName=@rolename and c.Role_Id not in (@ignoreId) LIMIT 0,1", new { rolename = rolename, ignoreId = ignoreId }).SingleOrDefault();
                if (i > 0) { return true; } else { return false; }
            }
        }
    }
}
