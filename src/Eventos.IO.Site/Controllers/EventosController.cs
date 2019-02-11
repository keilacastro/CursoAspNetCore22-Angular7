using Eventos.IO.Application.Interfaces;
using Eventos.IO.Application.ViewModels;
using Eventos.IO.Domain.Core.Notifications;
using Eventos.IO.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Eventos.IO.Site.Controllers
{
    [Route("")]
    public class EventosController : BaseController
    {
        private readonly IEventoAppService _eventoAppService;

        public EventosController(
            IEventoAppService eventoAppService,
            IDomainNotificationHandler<DomainNotification> notifications,
            IUser user) : base(notifications, user)
        {
            _eventoAppService = eventoAppService;
        }

        #region Evento

        [Route("")]
        [Route("proximos-eventos")]
        public IActionResult Index()
        {
            return View(_eventoAppService.ObterTodos());
        }

        [Route("meus-eventos")]
        [Authorize(Policy = "PodeConsultar")]
        public IActionResult MeusEventos()
        {
            return View(_eventoAppService.ObterEventosPorOrganizador(OrganizadorId));
        }

        [Route("dados-do-evento/{id:guid}")]
        public IActionResult Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventoViewModel = _eventoAppService.ObterPorId(id.Value);
            if (eventoViewModel == null)
            {
                return NotFound();
            }

            return View(eventoViewModel);
        }

        [Authorize(Policy = "PodeGravar")]
        [Route("novo-evento")]
        public IActionResult Create()
        {
            return View();
        }

        [Route("novo-evento")]
        [Authorize(Policy = "PodeGravar")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(EventoViewModel eventoViewModel)
        {
            if (!ModelState.IsValid) return View(eventoViewModel);

            eventoViewModel.OrganizadorId = OrganizadorId;
            _eventoAppService.Registrar(eventoViewModel);

            ViewBag.RetornoPost = OperacaoValida()
                ? "success|Evento registrado com sucesso!"
                : "error|Evento não registrado! Verifique as mensagens...";

            return View(eventoViewModel);
        }

        [Route("editar-evento/{id:guid}")]
        [Authorize(Policy = "PodeGravar")]
        public IActionResult Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventoViewModel = _eventoAppService.ObterPorId(id.Value);
            if (eventoViewModel == null)
            {
                return NotFound();
            }

            if (!ValidarAutoridadeEvento(eventoViewModel))
            {
                return RedirectToAction(nameof(MeusEventos), _eventoAppService.ObterEventosPorOrganizador(OrganizadorId));
            }

            return View(eventoViewModel);
        }

        [Route("editar-evento/{id:guid}")]
        [Authorize(Policy = "PodeGravar")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(EventoViewModel eventoViewModel)
        {
            if (!ValidarAutoridadeEvento(eventoViewModel))
            {
                return RedirectToAction(nameof(MeusEventos), _eventoAppService.ObterEventosPorOrganizador(OrganizadorId));
            }

            if (!ModelState.IsValid) return View(eventoViewModel);

            eventoViewModel.OrganizadorId = OrganizadorId;
            _eventoAppService.Atualizar(eventoViewModel);

            ViewBag.RetornoPost = OperacaoValida()
                ? "success|Evento atualizado com sucesso!"
                : "error|Evento não será atualizado. Verifique as mensagens";

            if (_eventoAppService.ObterPorId(eventoViewModel.Id).Online)
                eventoViewModel.Endereco = null;
            else
                eventoViewModel = _eventoAppService.ObterPorId(eventoViewModel.Id);

            return View(eventoViewModel);
        }

        [Route("excluir-evento/{id:guid}")]
        [Authorize(Policy = "PodeExcluir")]
        public IActionResult Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventoViewModel = _eventoAppService.ObterPorId(id.Value);
            if (eventoViewModel == null)
            {
                return NotFound();
            }

            if (ValidarAutoridadeEvento(eventoViewModel))
            {
                return RedirectToAction(nameof(MeusEventos), _eventoAppService.ObterEventosPorOrganizador(OrganizadorId));
            }

            return View(eventoViewModel);
        }

        [Route("excluir-evento/{id:guid}")]
        [Authorize(Policy = "PodeExcluir")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(Guid id)
        {
            if (ValidarAutoridadeEvento(_eventoAppService.ObterPorId(id)))
            {
                return RedirectToAction(nameof(MeusEventos), _eventoAppService.ObterEventosPorOrganizador(OrganizadorId));
            }

            _eventoAppService.Excluir(id);
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Endereço

        [Authorize(Policy = "PodeGravar")]
        [Route("incluir-endereco/{id:guid}")]
        public IActionResult IncluirEndereco(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventoViewModel = _eventoAppService.ObterPorId(id.Value);
            return PartialView("_IncluirEndereco", eventoViewModel);
        }

        [Authorize(Policy = "PodeGravar")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("incluir-endereco/{id:guid}")]
        public IActionResult IncluirEndereco(EventoViewModel eventoViewModel)
        {
            ModelState.Clear();

            eventoViewModel.Endereco.EventoId = eventoViewModel.Id;
            _eventoAppService.IncluirEndereco(eventoViewModel.Endereco);

            if (OperacaoValida())
            {
                string url = Url.Action("ObterEndereco", "Eventos", new { id = eventoViewModel.Id });
                return Json(new { success = true, url = url });
            }

            return PartialView("_IncluirEndereco", eventoViewModel);
        }

        [Authorize(Policy = "PodeGravar")]
        [Route("atualizar-endereco/{id:guid}")]
        public IActionResult AtualizarEndereco(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventoViewModel = _eventoAppService.ObterPorId(id.Value);
            return PartialView("_AtualizarEndereco", eventoViewModel);
        }

        [Authorize(Policy = "PodeGravar")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("atualizar-endereco/{id:guid}")]
        public IActionResult AtualizarEndereco(EventoViewModel eventoViewModel)
        {
            ModelState.Clear();
            _eventoAppService.AtualizarEndereco(eventoViewModel.Endereco);

            if (OperacaoValida())
            {
                string url = Url.Action("ObterEndereco", "Eventos", new { id = eventoViewModel.Id });
                return Json(new { success = true, url = url });
            }

            return PartialView("_AtualizarEndereco", eventoViewModel);
        }

        [Route("listar-endereco/{id:guid}")]
        public IActionResult ObterEndereco(Guid id)
        {
            return PartialView("_DetalhesEndereco", _eventoAppService.ObterEnderecoPorId(id));
        }

        #endregion

        #region Helper

        /// <summary>
        /// Verifica se o organizador que está editando.
        /// </summary>
        /// <param name="eventoViewModel"></param>
        /// <returns>Retorna true se igual e false se o organizador for diferente</returns>
        private bool ValidarAutoridadeEvento(EventoViewModel eventoViewModel)
        {
            return eventoViewModel.OrganizadorId == OrganizadorId;
        }
        
        #endregion
    }
}
