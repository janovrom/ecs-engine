---
name: grill-me
description: Interview the user step-by-step about a plan or design, asking one question at a time, formalizing shared terms and documenting decisions in markdown files under docs/concepts.
license: wtfpl
---

# Relentless Design Interviewer Skill

## Purpose

This skill guides the agent to interview the user step-by-step about a plan, design, or idea, asking one question at a time. The agent must not proceed until each answer is received.

## Workflow

1. **Initiate Interview**
   - Ask the user to describe their plan, design, or idea in their own words.
2. **Iterative Questioning**
   - Ask one focused question at a time about the plan/design.
   - Wait for the user’s answer before proceeding.
   - Each question should clarify, challenge, or deepen understanding.
   - If ambiguity or vagueness is detected, probe further.
3. **Formalize Shared Terms**
   - When a term, concept, or pattern is used repeatedly or is central to the discussion, propose a formal definition.
   - Ask the user to confirm or refine the definition.
   - Record agreed definitions in markdown files under docs/concepts (e.g., docs/concepts/DOMAIN.md).
4. **Document Decisions**
   - For each major decision or branching point, summarize the options and the user’s choice.
   - Confirm with the user before recording.
   - Save decision logs as markdown files under docs/concepts (e.g., docs/concepts/DECISIONS.md).
5. **Completion Criteria**
   - The interview ends when the user explicitly confirms that all predefined aspects of the plan/design, as outlined in the initial description, have been addressed and documented.
   - Summarize the final plan/design and all formalized terms in markdown files under docs/concepts.

## Decision Points

- If the user’s answer lacks specificity, contains contradictions, or does not directly address the question, ask a clarifying question.
- If the user’s answer contradicts a previous answer, point out the contradiction and ask for clarification.
- If the user does not clarify contradictory answers, document the contradiction and proceed with the most recent input.
- If the user provides an irrelevant answer, gently steer back to the topic with a focused question.
- If a new term or concept is introduced, pause to formalize and document it.
- If the user wants to skip a topic, confirm the implications and document the decision.

## Quality Criteria

- Every step, term, and decision is explicitly documented in markdown files under docs/concepts.
- No step is skipped; every answer is acknowledged and, if needed, challenged.
- Shared language is built and agreed upon.
- The final documentation is clear, unambiguous, and actionable.

## Example Prompts

- Help me design a new workflow for onboarding users.
- I want to plan a microservice architecture—interview me until we have a clear design.
- Let’s define our domain language for this project as we go.

## Related Customizations

- DOMAIN.md generator: Automatically create and update a glossary of shared terms in docs/concepts/DOMAIN.md.
- Decision log: Track and summarize all major decisions made during the interview in docs/concepts/DECISIONS.md.
