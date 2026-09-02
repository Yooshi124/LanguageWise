namespace LanguageWise.ChatDiscussionService.Api.Tests;

public sealed class HelpKnowledgeBaseTests
{
    [TestCase("How do I create a new post?", "create-post")]
    [TestCase("how do I edit my post", "edit-post")]
    [TestCase("Can I delete a post I wrote?", "delete-post")]
    [TestCase("how do I reply to someone", "comments")]
    [TestCase("what does the heart button do", "likes")]
    [TestCase("how do I search this forum", "search")]
    [TestCase("which forums are there", "forums")]
    [TestCase("how do I log out", "signing-in")]
    public void Retrieve_RanksTheArticleThatAnswersTheQuestionFirst(string question, string expectedId)
    {
        var articles = HelpKnowledgeBase.Retrieve(question);

        Assert.That(articles, Is.Not.Empty);
        Assert.That(articles[0].Id, Is.EqualTo(expectedId));
    }

    [Test]
    public void Retrieve_ReturnsAtMostTheConfiguredNumberOfArticles()
    {
        var articles = HelpKnowledgeBase.Retrieve("post comment like forum search edit delete");

        Assert.That(articles, Has.Count.LessThanOrEqualTo(HelpKnowledgeBase.RetrievedArticleLimit));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    [TestCase("of the it")]
    public void Retrieve_WithNothingToMatchOn_ReturnsNothing(string? question)
    {
        Assert.That(HelpKnowledgeBase.Retrieve(question), Is.Empty);
    }

    [Test]
    public void Retrieve_WithAnUnrelatedQuestion_ReturnsNothing()
    {
        Assert.That(HelpKnowledgeBase.Retrieve("what is the capital of Peru"), Is.Empty);
    }

    [Test]
    public void BuildContext_RendersEachArticleUnderItsTitle()
    {
        var articles = HelpKnowledgeBase.Retrieve("how do I create a new post");

        var context = HelpKnowledgeBase.BuildContext(articles);

        Assert.That(context, Does.Contain("## Creating a new post"));
        Assert.That(context, Does.Contain("New post"));
    }

    [Test]
    public void BuildContext_WithoutAnyArticles_IsEmpty()
    {
        Assert.That(HelpKnowledgeBase.BuildContext([]), Is.Empty);
    }

    [Test]
    public void Articles_HaveUniqueIds()
    {
        var ids = HelpKnowledgeBase.Articles.Select(article => article.Id).ToList();

        Assert.That(ids, Is.Unique);
    }
}
