using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;
using System.Diagnostics.Eventing.Reader;

namespace ToDoList.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        public List<TaskInfo> listTasks = new List<TaskInfo>();
        public string CurrentFilter { get; set; } = string.Empty;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet(string filter = "all")
        {
            string connectionString = "Data Source=.\\sqlexpress;Initial Catalog=todolist;Integrated Security=True";
            listTasks = new List<TaskInfo>();
            CurrentFilter = filter == "active" ? "Active" : "All";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string sql = $"SELECT id, title, description, deadline, is_finished, is_active FROM tasks";

                if (filter == "active")
                {
                    sql += " WHERE is_active = 1 AND deadline >= @Today";
                }

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    if (filter == "active")
                    {
                        command.Parameters.AddWithValue("@Today", DateTime.Now);
                    }

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DateTime deadline = reader.GetDateTime(3);
                            bool isActive = reader.GetBoolean(5);
                            if (DateTime.Now > deadline && isActive)
                            {
                                isActive = false;
                                UpdateTaskIsActive(reader.GetInt32(0), isActive);
                            }

                            TaskInfo taskInfo = new TaskInfo
                            {
                                Id = reader.GetInt32(0).ToString(),
                                Title = reader.GetString(1),
                                Description = reader.GetString(2),
                                Deadline = deadline.ToString(),
                                IsFinished = reader.GetBoolean(4),
                                IsActive = isActive
                            };
                            listTasks.Add(taskInfo);
                        }
                    }
                }
            }
        }

        private void UpdateTaskIsActive(int taskId, bool isActive)
        {
            string connectionString = "Data Source=.\\sqlexpress;Initial Catalog=todolist;Integrated Security=True";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string sql = "UPDATE tasks SET is_active = @IsActive WHERE id = @Id";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@IsActive", isActive);
                    command.Parameters.AddWithValue("@Id", taskId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public async Task<JsonResult> OnPostToggleIsFinishedAsync(int taskId)
        {
            string connectionString = "Data Source=.\\sqlexpress;Initial Catalog=todolist;Integrated Security=True";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string fetchSql = "SELECT is_finished FROM tasks WHERE id = @Id";
                bool currentIsFinished;

                using (SqlCommand fetchCommand = new SqlCommand(fetchSql, connection))
                {
                    fetchCommand.Parameters.AddWithValue("@Id", taskId);
                    using (var reader = await fetchCommand.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            currentIsFinished = reader.GetBoolean(0);
                        }
                        else
                        {
                            return new JsonResult(new { Error = "Task not found" });
                        }
                    }
                }

                string updateSql = "UPDATE tasks SET is_finished = @IsFinished WHERE id = @Id";
                bool newIsFinished = !currentIsFinished;

                using (SqlCommand updateCommand = new SqlCommand(updateSql, connection))
                {
                    updateCommand.Parameters.AddWithValue("@IsFinished", newIsFinished);
                    updateCommand.Parameters.AddWithValue("@Id", taskId);
                    int affectedRows = await updateCommand.ExecuteNonQueryAsync();
                    if (affectedRows > 0)
                        return new JsonResult("Success");
                    else
                        return new JsonResult(new { Error = "Update failed, no rows affected." });
                }
            }
        }

        public async Task<JsonResult> OnPostDeleteTaskAsync(int taskId)
        {
            string connectionString = "Data Source=.\\sqlexpress;Initial Catalog=todolist;Integrated Security=True";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM tasks WHERE id = @Id";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", taskId);
                    int affectedRows = await command.ExecuteNonQueryAsync();
                    if (affectedRows > 0)
                        return new JsonResult("Success");
                    else
                        return new JsonResult("Error");
                }
            }
        }

        public async Task<IActionResult> OnPostCreateTaskAsync(string taskName, string taskDesc, DateTime taskDead)
        {
            string connectionString = "Data Source=.\\sqlexpress;Initial Catalog=todolist;Integrated Security=True";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "INSERT INTO tasks (title, description, deadline, is_finished, is_active) VALUES (@Title, @Description, @Deadline, 'False', 'True')";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Title", taskName);
                    command.Parameters.AddWithValue("@Description", taskDesc);
                    command.Parameters.AddWithValue("@Deadline", taskDead);
                    await command.ExecuteNonQueryAsync();
                }
            }

            return RedirectToPage();
        }
    }

    public class TaskInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Deadline { get; set; } = string.Empty;
        public bool IsFinished { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }

}