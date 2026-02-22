using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JLSDataAccess.Interfaces;
using JLSDataModel.Models.Message;
using Microsoft.EntityFrameworkCore;

namespace JLSDataAccess.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly JlsDbContext db;

    public MessageRepository(JlsDbContext context)
    {
        db = context;
    }

    public async Task<long> CreateMessage(Message message, int? FromUser, int? ToUser)
    {
        message.CreatedOn = DateTime.Now;
        db.Add(message);
        await db.SaveChangesAsync();
        if (message.Id > 0 && (FromUser != null || ToUser != null))
        {
            var messageDestination = new MessageDestination();
            messageDestination.MessageId = message.Id;
            messageDestination.FromUserId = FromUser != null ? FromUser : null;
            messageDestination.ToUserId = ToUser != null ? ToUser : null;

            await db.AddAsync(messageDestination);
            await db.SaveChangesAsync();
        }

        return message.Id;
    }

    public async Task<List<dynamic>> GetMessageByUserAndStatus(int ToUserId, bool? IsReaded)
    {
        var result = await (from m in db.Message
            join md in db.MessageDestination on m.Id equals md.MessageId
            where md.ToUserId == ToUserId && (IsReaded == null || m.IsReaded == IsReaded)
            orderby m.CreatedOn descending
            select new
            {
                m.Id,
                m.IsReaded,
                md.FromUserId,
                ToUserId = md.FromUserId,
                m.Title,
                m.Body,
                m.CreatedOn
            }).ToListAsync<dynamic>();

        return result;
    }

    public async Task<long> UpdateMessageStatus(long MessageId, bool Status, int? UserId)
    {
        var Message = db.Message.Find(MessageId);
        if (Message != null)
        {
            Message.UpdatedBy = UserId;
            Message.IsReaded = Status;

            db.Update(Message);
            await db.SaveChangesAsync();
            return Message.Id;
        }

        return 0;
    }
}