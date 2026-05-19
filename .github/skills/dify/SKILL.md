---
name: dify
description: Guides the user to perform trivial or repetitive changes themselves, providing step-by-step instructions or commands instead of making the changes directly.
license: WTFPL
---

## Purpose
This skill is for situations where the requested change is trivial, repetitive, or best learned by doing. Instead of making the change automatically, the agent will:
- Explain why the change is trivial or repetitive
- Output the exact steps or commands for the user to follow
- Optionally, provide a checklist or template for similar future changes
- Encourage the user to perform the change themselves (Do-It-Yourself)

## Workflow
1. **Assess Request**: Determine if the user's request is trivial, repetitive, or educationally valuable to do manually.
2. **Explain Reasoning**: Briefly explain why the agent will not perform the change directly.
3. **Output Steps/Commands**: Provide clear, actionable steps or shell/editor commands for the user to follow.
4. **Completion Check**: Suggest how the user can verify the change is correct.
5. **Encourage Learning**: Optionally, provide tips or resources for similar tasks.

## Decision Points
- If the change is non-trivial, risky, or error-prone, do NOT use this skill—perform the change or escalate.
- If the user insists on automation, escalate to a normal agent workflow.

## Quality Criteria
- Steps are clear, concise, and actionable
- User is not overwhelmed with too much information
- Reasoning is respectful but direct
- User is empowered to learn or repeat the process
- Completion check is provided

## Example Prompts
- "DIFY: Show me how to rename a file in the terminal."
- "DIFY: I want to add a line to my README manually."
- "DIFY: Give me the steps to fix a typo in my code."

## Related Customizations to Consider
- A skill for onboarding new contributors with hands-on tasks
- A checklist skill for common manual fixes
- A skill for teaching shell or editor basics
