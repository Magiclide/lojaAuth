﻿using Loja.Attributes;
using Loja.Data;
using Loja.Models;
using Loja.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Loja.Controllers
{
    [ApiController]
    public class ProductController : ControllerBase
    {
        [Authorize(Roles = "adm")]
        [HttpPost("product")]
        public async Task<IActionResult> PostAsync(
            [FromServices] AppDbContext context,
            [FromBody] ProductCreateViewModel model)
        {
            try
            {
                var category = await context.Categories.FindAsync(model.CategoryId);

                if (category == null) 
                    return NotFound(new {message = "Categoria não existe"});


                var fileName = $"{Guid.NewGuid().ToString()}.jpg";
                
                var data = new Regex(@"^data:image\/[a-z]+;base64,")
                    .Replace(model.Base64Image, "");

                var bytes = Convert.FromBase64String(data);

                await System.IO.File
                    .WriteAllBytesAsync($"wwwroot/images/{fileName}", bytes);


                var productNew = new Product
                {
                    Name = model.Name,
                    Value = model.Value,
                    Description = model.Description,
                    Image = $"https://localhost:7037/images/{fileName}",
                    Category = category
                };

                await context.Products.AddAsync(productNew);
                await context.SaveChangesAsync();

                return Ok();
            }
            catch
            {
                return StatusCode(500, new { message = "Erro no servidor" });
            }
        }



        [Authorize(Roles = "cliente, adm")]
        [HttpGet("product")]
        public async Task<IActionResult> GetAsync(
            [FromServices] AppDbContext context,
            [FromQuery] int page = 0, // <----
            [FromQuery] int pageSize = 10) // <----
        {
            try
            {
                var count = await context.Products
                    .AsNoTracking()
                    .CountAsync();

                var products = await context.Products
                    .AsNoTracking()
                    .Include(x => x.Category)
                    .Skip(page * pageSize) // <----
                    .Take(pageSize) // <----
                    .ToListAsync();

                return Ok(new { 
                    count,
                    page,
                    pageSize,
                    products 
                });
            }
            catch
            {
                return StatusCode(500, new { message = "Erro no servidor" });
            }
        }



        [Authorize(Roles = "adm")]
        [HttpPut("product/{id:int}")]
        public async Task<IActionResult> PutAsync(
            [FromServices] AppDbContext context,
            [FromBody] ProductUpdateViewModel model,
            [FromRoute] int id)
        {
            try
            {
                var product = await context.Products.FindAsync(id);

                if (product == null)
                    return NotFound(new { message = "Produto não existe" });


                var category = await context.Categories
                    .FindAsync(model.CategoryId);

                if (category == null)
                    return NotFound(new { message = "Categoria não existe" });


                product.Name = model.Name;
                product.Description = model.Description;
                product.Category = category;
                product.Value = model.Value;
                product.Image = model.Image;

                await context.SaveChangesAsync();

                return Ok();
            }
            catch
            {
                return StatusCode(500, new { message = "Erro no servidor" });
            }
        }

        [Authorize(Roles = "adm")]
        [HttpDelete("product/{id:int}")]
        public async Task<IActionResult> DeleteAsync(
            [FromServices] AppDbContext context,            
            [FromRoute] int id)
        {
            try
            {
                var product = await context.Products.FindAsync(id);

                if (product == null)
                    return NotFound(new { message = "Produto não existe" });
                
                context.Products.Remove(product);
                await context.SaveChangesAsync();

                return Ok();
            }
            catch
            {
                return StatusCode(500, new { message = "Erro no servidor" });
            }
        }




        [ApiKey]
        [HttpGet("product/revendedor")]
        public async Task<IActionResult> GetRevendedorAsync(
            [FromServices] AppDbContext context)
        {
            try
            {
                var products = await context.Products
                    .Include(x => x.Category)
                    .Select(x => new {
                        x.Name,
                        x.Description,
                        value = x.Value - (decimal)((double)x.Value * 0.15),
                        x.Image,
                        x.Category
                    })
                    .ToListAsync();

                return Ok(products);
            }
            catch
            {
                return StatusCode(500, new { message = "Erro no servidor" });
            }
        }
    }
}
