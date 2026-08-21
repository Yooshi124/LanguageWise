namespace ChatDiscussion.Models
{
    public class Post
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        //public User User { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public List<Comment> Comments { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}