﻿using CasaDoCodigo.Models;
using CasaDoCodigo.Models.ViewModels;
using CasaDoCodigo.Services;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly.CircuitBreaker;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CasaDoCodigo.Controllers
{
    class ApiUris
    {
        public static string GetProdutos => "api/produto";
        public static string GetCarrinho => "api/carrinho";
        public static string GetPedido => "api/pedido";
        public static string UpdateQuantidade => "api/carrinho";
        public static string UpdateCadastro => "api/cadastro";
    }

    public class PedidoController : Controller
    {
        private readonly ILogger logger;
        private readonly IHttpContextAccessor contextAccessor;
        private readonly HttpClient httpClient;
        private readonly IApiService apiService;
        private readonly ICarrinhoService carrinhoService;
        private readonly ISessionHelper sessionHelper;

        public IConfiguration Configuration { get; }

        public PedidoController(ILogger<PedidoController> logger,
            IHttpContextAccessor contextAccessor,
            HttpClient httpClient,
            ISessionHelper sessionHelper,
            IConfiguration configuration,
            ICarrinhoService carrinhoService,
            IApiService apiService)
        {
            this.logger = logger;
            this.contextAccessor = contextAccessor;
            this.httpClient = httpClient;
            this.sessionHelper = sessionHelper;
            this.Configuration = configuration;
            this.carrinhoService = carrinhoService;
            this.apiService = apiService;
        }

        public async Task<IActionResult> Carrossel()
        {
            try
            {
                return View(await apiService.GetProdutos());
            }
            catch (BrokenCircuitException e)
            {
                HandleBrokenCircuitException();
            }
            catch (Exception e)
            {
                HandleBrokenCircuitException();
            }

            return View();
        }

        [Authorize]
        public async Task<IActionResult> Carrinho(string codigo)
        {
            try
            {
                string idUsuario = GetUserId();
                int pedidoId = GetPedidoId() ?? 0;
                ItemCarrinho itemCarrinho = new ItemCarrinho(codigo, codigo, $"produto código {codigo}", 1.23m, 1, "");
                var carrinho = await carrinhoService.UpdateItem(idUsuario, itemCarrinho);
                return View(carrinho);
            }
            catch (BrokenCircuitException)
            {
                HandleBrokenCircuitException();
            }
            catch (Exception e)
            {
                HandleBrokenCircuitException();
            }
            return View();
        }

        private string GetUserId()
        {
            var claims = User.Claims;
            var userIdClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject);
            if (userIdClaim == null)
            {
                userIdClaim = claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            }
            if (userIdClaim == null)
            {
                throw new Exception("Usuário desconhecido");
            }

            var idUsuario = userIdClaim.Value;
            return idUsuario;
        }

        public async Task<IActionResult> Cadastro()
        {
            try
            {
                int pedidoId = GetPedidoId() ?? throw new ArgumentNullException("pedidoId");
                PedidoViewModel pedido = await apiService.GetPedido(pedidoId);

                if (pedido == null)
                {
                    return RedirectToAction("Carrossel");
                }

                return View(pedido.Cadastro);
            }
            catch (BrokenCircuitException)
            {
                HandleBrokenCircuitException();
            }
            catch (Exception e)
            {
                HandleBrokenCircuitException();
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resumo(Cadastro cadastro)
        {
            if (ModelState.IsValid)
            {
                var viewModel = new CasaDoCodigo.Models.CadastroViewModel(cadastro);
                viewModel.PedidoId = GetPedidoId().Value;
                var pedidoViewModel = await apiService.UpdateCadastro(viewModel);
                return base.View(pedidoViewModel);
            }
            return RedirectToAction("Cadastro");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<UpdateQuantidadeOutput> UpdateQuantidade([FromBody]ItemPedido itemPedido)
        {
            return await apiService.UpdateQuantidade(itemPedido.Id.ToString("000"), itemPedido.Quantidade);
        }

        public async Task Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            await HttpContext.SignOutAsync("oidc");
        }

        private int? GetPedidoId()
        {
            return contextAccessor.HttpContext.Session.GetInt32("pedidoId");
        }

        private void SetPedidoId(int pedidoId)
        {
            contextAccessor.HttpContext.Session.SetInt32("pedidoId", pedidoId);
        }

        private void HandleBrokenCircuitException()
        {
            ViewBag.MsgCarrosselIndisponivel = "O serviço de carrossel não está ativo, por favor tente novamente mais tarde.";
        }
    }
}
