# StickyBoard — Board Structure & Section Hierarchy Model

## Overview

StickyBoard organizes collaborative content using a structured yet flexible workspace model designed to support:

- Sticky-note style brainstorming
- Column-based workflows (Kanban, etc.)
- Nested grouping / visual clustering
- Multi-view boards (Kanban, Calendar, Timeline, etc.)
- Scrollable sections instead of infinite whiteboards
- Automatic intelligent layout

This hybrid design preserves **clarity**, prevents users from losing content, and keeps the UX intuitive while supporting advanced behaviors like drawing divisions to form sub-sections.

------

## Core Concepts

### Board

A workspace that holds multiple **tabs**.
 Represents a project, space, or collaborative board.

A board always has **at least one tab**.

------

### Tab

A **view mode** or perspective on the board.

Examples:

- `kanban`
- `calendar`
- `timeline`
- `whiteboard`
- `custom`

Tabs allow users to organize and view the same card dataset in different ways.

Each tab has **at least one section** (a root container).

------

### Section

A **logical grouping container** for cards.
 Sections can be:

- Top-level (the tab’s root section, invisible unless no children exist)
- Nested (sub-sections created by the user)
- Shown horizontally or vertically depending on the tab type
- Scrollable if content exceeds available space

**Sections form a tree**, not a graph:

```
Root Section (invisible when child sections exist)
 ├── Section A
 │     ├── Sub A1
 │     └── Sub A2
 └── Section B
```

Sections never own tabs. Tabs own sections.

------

### Cards

The fundamental content unit.

- Must belong to a **tab**
- May optionally belong to a **section**
- If not assigned to a section, they belong to the **Ungrouped** container

This supports free-floating brainstorming **and** structured workflows.

------

## “Ungrouped” Behavior

### Purpose

When cards exist in a parent section and the user creates sub-sections, the parent section becomes invisible and an `Ungrouped` bucket appears automatically.

This prevents card loss and keeps semantics clear.

### Example

Initial:

```
[ Main ]
Card A
Card B
Card C
```

User adds first section `Ideas`:

```
[ Ideas ]   [ Ungrouped ]
Card A      Card B, Card C
```

Then user splits again:

```
[ Ideas ] [ Research ] [ Ungrouped ]
```

When Ungrouped empties, it disappears automatically.

------

## Why This Model Works

### UX Benefits

| Capability                     | Supported |
| ------------------------------ | --------- |
| No infinite zoom or panning    | ✅         |
| Scrollable lanes when needed   | ✅         |
| Auto-layout adapts to sections | ✅         |
| User can cluster visually      | ✅         |
| Cards never get “lost”         | ✅         |
| Beginner friendly              | ✅         |
| Power-user nesting available   | ✅         |

### Technical Benefits

| Benefit                              | Result                       |
| ------------------------------------ | ---------------------------- |
| Clear hierarchy                      | Board → Tab → Section → Card |
| No recursive tab/section loop        | ✅                            |
| Stable DB model                      | ✅                            |
| Simple tree traversal                | ✅                            |
| No orphan cards                      | ✅                            |
| Compatible with future drawing tools | ✅                            |

------

## User Actions → System Behavior

| User Action                 | System Response                                              |
| --------------------------- | ------------------------------------------------------------ |
| Create first section in tab | Convert root to invisible parent, show `[New Section] + [Ungrouped]` |
| Split a section             | Create sub-sections, move cards to Ungrouped                 |
| Drag a card into a section  | Assign `section_id`                                          |
| Remove all sections         | Show root section again                                      |
| Resize/view overflow        | Scroll section/container                                     |

------

## Data Model Summary

| Entity  | Required Links                     |
| ------- | ---------------------------------- |
| Board   | 1..* Tabs                          |
| Tab     | 1..* Sections (root always exists) |
| Section | 0..* Sub-sections                  |
| Card    | 1 Tab, 0..1 Section                |

### DB Relationships

| Field                        | Meaning                      |
| ---------------------------- | ---------------------------- |
| `tabs.board_id`              | Tab belongs to board         |
| `sections.tab_id`            | Section belongs to tab       |
| `sections.parent_section_id` | Section hierarchy            |
| `cards.tab_id`               | Card belongs to view context |
| `cards.section_id`           | Optional placement bucket    |

------

## Design Philosophy

This system embodies:

- **Structure when you need it**
- **Flexibility when you want it**
- **Simplicity for new users**
- **Power for complex workflows**

It delivers the usability of Trello, the grouping power of Notion, and the layout flexibility of Miro — **without infinite canvas cognitive overload**.