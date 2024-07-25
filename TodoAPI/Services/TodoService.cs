using Microsoft.EntityFrameworkCore;
using TodoAPI.Data_Context;
using TodoAPI.DTOs;
using TodoAPI.Models;

namespace TodoAPI.Services;

public class TodoService : ITodoService
{
    private readonly TodoDbContext _database;

    public TodoService(TodoDbContext dbContext)
    {
        _database = dbContext;
    }
    
    public async Task CreateTodo(int userId, TodoDto newTodo)
    {
        var todo = new Todo()
        {
            UserId = userId,
            Title = newTodo.Title,
            Description = newTodo.Description,
            Status = newTodo.Status
        };

        await _database.Todos.AddAsync(todo);
        await UpdateTodoCount(userId, true);
        await _database.SaveChangesAsync();
    }

    public async Task<List<Todo>> GetAllTodo(int userId)
    {
        var todos = await _database.Todos
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Updated)
            .ToListAsync();

        return todos;
    }

    public TodoPage GetTodoByPage(int userId, int page, int pageSize)
    {
        var todos = GetAllTodo(userId).Result;
        
        var todoPage = new TodoPage()
        {
            TotalPages = (int)Math.Ceiling(todos.Count / (double)pageSize), // calculate total page
            CurrentPage = page,
            Todos = todos.Skip(--page * pageSize).Take(pageSize).ToList()
        };

        return todoPage;
    }

    public async Task<Todo?> GetTodo(int userId, int todoId)
    {
        var todo = await _database.Todos.FirstOrDefaultAsync(t => t.UserId == userId && t.Id == todoId);

        return todo;
    }

    public async Task<Todo?> UpdateTodo(int userId, int todoId, TodoDto todoDto)
    {
        var todo = GetTodo(userId, todoId).Result;

        if (todo is null)
            return null;
        
        todo.Title = todoDto.Title;
        todo.Description = todoDto.Description;
        todo.Status = todoDto.Status;
        todo.Updated = DateTime.Now.AddSeconds(-DateTime.Now.Second).AddMilliseconds(-DateTime.Now.Millisecond);

        var newTodo = _database.Todos.Update(todo);
        await _database.SaveChangesAsync();

        return newTodo.Entity;
    }

    public async Task DeleteTodo(int userId, int todoId)
    {
        var todo = GetTodo(userId, todoId).Result;

        if (todo is null)
            return;
        
        _database.Todos.Remove(todo);
        await UpdateTodoCount(userId);
        await _database.SaveChangesAsync();
    }

    private async Task UpdateTodoCount(int userId, bool increment = false)
    {
        var user = await _database.Users.FindAsync(userId);
        if (increment)
            user!.TodoCount += 1;
        else
            user!.TodoCount -= 1;

        _database.Users.Update(user);
    }
    
}