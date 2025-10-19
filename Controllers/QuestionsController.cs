using BilbakalimAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BilbakalimAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QuestionsController> _logger;

    public QuestionsController(ApplicationDbContext context, ILogger<QuestionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Question>>> GetAllQuestions()
    {
        _logger.LogInformation("Fetching all questions.");
        var questions = await _context.Questions.ToListAsync();
        _logger.LogInformation($"Found {questions.Count} questions.");
        return questions;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Question>> GetQuestionById(int id)
    {
        var question = await _context.Questions.FindAsync(id);
        if (question == null)
        {
            return NotFound();
        }
        return question;
    }

    [HttpGet("{cityId}")]
    public async Task<ActionResult<IEnumerable<Question>>> GetQuestionsByCity(string cityId)
    {
        var cityQuestions = await _context.Questions
            .Where(q => EF.Functions.Like(q.CityId, cityId))
            .ToListAsync();

        if (cityQuestions == null || !cityQuestions.Any())
        {
            return NotFound(); // Şehirle ilgili soru bulunamazsa 404 döner
        }

        return Ok(cityQuestions);
    }

    [HttpPost]
    public async Task<ActionResult<Question>> AddQuestion([FromBody] QuestionDto questionData)
    {
        _logger.LogInformation($"Adding new question: {questionData.QuestionText}");
        var newQuestion = new Question(0, questionData.CityId, questionData.QuestionText, questionData.MediaUrl, questionData.Options, questionData.CorrectAnswerIndex);
        
        _context.Questions.Add(newQuestion);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Added new question with ID: {newQuestion.Id}");

        return CreatedAtAction(nameof(GetQuestionById), new { id = newQuestion.Id }, newQuestion);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateQuestion(int id, [FromBody] QuestionDto questionData)
    {
        var question = await _context.Questions.FindAsync(id);
        if (question == null)
        {
            return NotFound();
        }

        // Gelen verilerle yeni bir rekor oluşturmak yerine, mevcut varlığı güncelleyin.
        var updatedQuestion = question with
        {
            CityId = questionData.CityId,
            QuestionText = questionData.QuestionText,
            MediaUrl = questionData.MediaUrl,
            Options = questionData.Options,
            CorrectAnswerIndex = questionData.CorrectAnswerIndex
        };

        _context.Entry(question).CurrentValues.SetValues(updatedQuestion);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Questions.Any(e => e.Id == id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        var question = await _context.Questions.FindAsync(id);
        if (question == null)
        {
            return NotFound();
        }

        _context.Questions.Remove(question);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}