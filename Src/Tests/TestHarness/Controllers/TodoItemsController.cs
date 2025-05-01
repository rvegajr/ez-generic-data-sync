using Ez.Generic.DataSync.Core;
using Microsoft.AspNetCore.Mvc;

namespace Ez.Generic.DataSync.TestHarness.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TodoItemsController : SyncController<TodoItem>
    {
        public TodoItemsController(IRepository<SyncableEntityWrapper<TodoItem>> repository) 
            : base(repository)
        {
        }
    }
}
