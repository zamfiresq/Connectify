﻿using System.ComponentModel.DataAnnotations;

namespace Connectify.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Your message must have a content!")]
        public string TextMessage { get; set; }
        public DateTime SentAt { get; set; }

        // FK
        public int GroupId { get; set; }
        public Group Group { get; set; }

        // de adaugat user id 

    }
}