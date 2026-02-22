using System;
using System.Linq;
using System.Threading.Tasks;
using JLSApplicationBackend.Services;
using JLSDataAccess.Interfaces;
using JLSDataModel.Models.Message;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JLSMobileApplication.Controllers;

[Authorize]
[Route("api/[controller]/{action}/{id?}")]
[ApiController]
public class MessageController(
    IMessageRepository messageRepository,
    ISendEmailAndMessageService sendMessageService,
    ILogger<MessageController> logger)
    : Controller
{
    [AllowAnonymous]
    [HttpPost]
    public async Task<long> SaveMessage([FromBody] SaveMessageCriteria criteria)
    {
        try
        {
            var result =
                await messageRepository.CreateMessage(criteria.Message, criteria.FromUserId, criteria.ToUserId);
            if (criteria.ToUserId == null)
                await sendMessageService.ClientMessageToAdminAsync(criteria.Message.SenderEmail, criteria.Message.Body);

            return result;
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetMessageByUserAndStatus(int UserId, bool? Status, int Step, int Begin)
    {
        try
        {
            var result = await messageRepository.GetMessageByUserAndStatus(UserId, Status);
            return Json(new
            {
                TotalCount = result.Count,
                List = result.Skip(Begin * Step).Take(Step).ToList()
            });
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }

    [HttpPost]
    public async Task<long> UpdateMessageStatus([FromBody] UpdateMessageStatusCriteria criteria)
    {
        try
        {
            return await messageRepository.UpdateMessageStatus(criteria.MessageId, criteria.Status, criteria.UserId);
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }


    [HttpGet]
    public async Task<int> GetNoReadMessageCount(int UserId)
    {
        try
        {
            var result = await messageRepository.GetMessageByUserAndStatus(UserId, false);
            return result.Count();
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }

    public class SaveMessageCriteria
    {
        public int? FromUserId { get; set; }

        public int? ToUserId { get; set; }

        public Message Message { get; set; }
    }


    public class UpdateMessageStatusCriteria
    {
        public long MessageId { get; set; }
        public bool Status { get; set; }
        public int? UserId { get; set; }
    }
}