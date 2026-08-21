namespace ChatDiscussion.Models
{
    public class Image
    {
        public int Id { get; set; }
        public Post Post { get; set; }
        public int PostId { get; set; }
        public Comment Comment { get; set; }
        public int CommentId { get; set; }
        public int UserId { get; set; }
        //public User User { get; set; }
        public string FileUrl { get; set; }
        public string FileName { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}