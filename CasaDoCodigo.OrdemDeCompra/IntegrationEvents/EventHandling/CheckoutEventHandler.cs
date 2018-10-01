﻿using CasaDoCodigo.Mensagens.Events;
using CasaDoCodigo.OrdemDeCompra.Commands;
using CasaDoCodigo.OrdemDeCompra.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Rebus.Handlers;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CasaDoCodigo.Mensagens.EventHandling
{
    public class CheckoutEventHandler : IHandleMessages<CheckoutEvent>
    {
        private readonly IMediator _mediator;

        public CheckoutEventHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        Task IHandleMessages<CheckoutEvent>.Handle(CheckoutEvent message)
        {
            Trace.WriteLine("Received Checkout. Message Follows");
            Trace.WriteLine("----------------------------------");
            Trace.WriteLine(message.ClienteId);
            foreach (var item in message.Itens)
            {
                Trace.WriteLine(
                $"Id = {item.Id}, " +
                $"ProdutoId = {item.ProdutoId}, " +
                $"ProdutoNome = {item.ProdutoNome}, " +
                $"PrecoUnitario = {item.PrecoUnitario}, " +
                $"Quantidade = {item.Quantidade}, " +
                $"UrlImagem = {item.UrlImagem}, ");
            }
            Trace.WriteLine("----------------------------------");
            Trace.WriteLine("Message Ends");

            var itens = message.Itens.Select(
                    i => new CreatePedidoCommandItem(0, "", "", i.PrecoUnitario, i.Quantidade, i.PrecoUnitario)
                ).ToList();

            var createPedidoCommand = new CreatePedidoCommand(itens, "", "", "", "", "", "", "", "", "");

            var requestCreateOrder = new IdentifiedCommand<CreatePedidoCommand, bool>(createPedidoCommand, message.Id);

            //HACK: I should never call Wait(), but this method override cannot be async...
            _mediator.Send(requestCreateOrder);
            return Task.CompletedTask;
        }
    }
}