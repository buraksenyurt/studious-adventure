using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ToyApi.Db;
using ToyApi.Db.Models;

namespace ToyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToyController : ControllerBase
    {
        private readonly IToyRepository _toyRepository;
        private readonly IHubContext<ToyApiHub> _hubContext;

        public ToyController(IToyRepository toyRepository, IHubContext<ToyApiHub> hubContext)
        {
            _toyRepository = toyRepository;
            _hubContext = hubContext;
        }

        [HttpGet()]
        [Route("TopFive")]
        public IActionResult GetTopFive()
        {
            var topFive = _toyRepository.GetTopFive();
            return Ok(topFive);
        }

        [HttpPost]
        public IActionResult Create([FromBody] Toy toy)
        {
            var inserted = _toyRepository.Create(toy);
            if (inserted > 0)
            {
                _hubContext.Clients.All.SendAsync("NotifyNewToyAdded", toy.ToyId, toy.Nickname);
                return Ok("New toy has been added successfully.");
            }

            return BadRequest();
        }

        [HttpPut]
        public IActionResult Update([FromBody] Toy toy)
        {
            var updated = _toyRepository.Update(toy);
            if (updated != null)
            {
                return Ok("Toy has been updated successfully.");
            }
            else
            {
                return NotFound();
            }
        }
    }
}
