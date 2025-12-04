using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Order.API.Models;
using Order.API.utils;

namespace Order.API.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class OrderController : ControllerBase
    {

        private readonly ApplicationDbContext _context;
        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            RabbitMQProducer producer = new RabbitMQProducer(new ConfigurationBuilder().AddJsonFile("appsettings.json").Build());
            //var jwt = Request.Headers["Authorization"].ToString();
            //Console.WriteLine("Received JWT: " + jwt);
            await producer.SendMessageAsync("ProductCreatedQueue : id" + Guid.NewGuid().ToString() + " status : " + true);
            //await CreateOrder("Sample Product", 500);
            return Ok("Order Service is running");
        }

        public async Task CreateOrder(string product, decimal price)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = new ProductOrder
                {
                    Product = product,
                    Price = price
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Yapay bir hata oluşturalım
                if (price > 1000)
                    throw new Exception("Fiyat çok yüksek!");

                _context.OutboxMessages.Add(new OutboxMessage
                {
                    EventType = "OrderCreated",
                    Payload = JsonSerializer.Serialize(order)
                });

                await _context.SaveChangesAsync();

                // Transaction başarılı → Commit edilir
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                // Burada rollback zorunlu değil, ama yazmak daha okunaklıdır
                await transaction.RollbackAsync();

                Console.WriteLine($"Rollback yapıldı: {ex.Message}");
            }
        }


    }
}