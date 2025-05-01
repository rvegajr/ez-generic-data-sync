using Ez.Generic.DataSync.Core;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ez.Generic.DataSync.TestHarness.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyncController<T> : ControllerBase where T : class
    {
        private readonly IRepository<SyncableEntityWrapper<T>> _repository;
        
        public SyncController(IRepository<SyncableEntityWrapper<T>> repository)
        {
            _repository = repository;
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SyncableEntityWrapper<T>>>> GetAll()
        {
            var items = await _repository.GetItemsAsync();
            return Ok(items);
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<SyncableEntityWrapper<T>>> Get(string id)
        {
            var item = await _repository.GetItemAsync(id);
            if (item == null)
                return NotFound();
            return Ok(item);
        }
        
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SyncableEntityWrapper<T> item)
        {
            await _repository.AddItemAsync(item, item.Id);
            return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] SyncableEntityWrapper<T> item)
        {
            await _repository.UpdateItemAsync(item, id);
            return NoContent();
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _repository.DeleteItemAsync(id);
            return NoContent();
        }
        
        [HttpPost("pull")]
        public async Task<ActionResult<SyncResult>> Pull()
        {
            // Simulate a pull operation
            var items = await _repository.GetItemsAsync();
            return Ok(new SyncResult
            {
                Status = SyncStatus.Completed,
                ItemCount = items.Count()
            });
        }
        
        [HttpPost("push")]
        public async Task<ActionResult<SyncResult>> Push([FromBody] IEnumerable<SyncableEntityWrapper<T>> items)
        {
            // Simulate a push operation
            foreach (var item in items)
            {
                if (item.Deleted)
                {
                    await _repository.DeleteItemAsync(item.Id);
                }
                else
                {
                    var existingItem = await _repository.GetItemAsync(item.Id);
                    if (existingItem == null)
                    {
                        await _repository.AddItemAsync(item, item.Id);
                    }
                    else
                    {
                        await _repository.UpdateItemAsync(item, item.Id);
                    }
                }
            }
            
            return Ok(new SyncResult
            {
                Status = SyncStatus.Completed,
                ItemCount = items.Count()
            });
        }
    }
}
