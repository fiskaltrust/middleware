- Feature Name: (fill me in with a unique ident. e.g. `my_feature`)
- Start Date: (fill me in with today's date. e.g. YYYY-MM-DD)
- RFC PR: [fiskaltrust/middleware#0000](https://github.com/fiskaltrust/middleware/pull/0000)
- Tracking Issue: [fiskaltrust/middleware#0000](https://github.com/fiskaltrust/middleware/issues/0000)
- Markets: (fill me in with the markets that this feature applies to. e.g. `XX`,`YY`)

# Summary

One paragraph explanation of the feature.

# Motivation

Why are we doing this? What use cases does it support? What is the expected outcome?

# Guide-level explanation

Explain the proposal as if it was already included in the middleware and you were teaching it to a PosCreator. That generally means:

- Introducing new named concepts.
- Explaining the feature, ideally through simple examples of solutions to concrete problems.
- Explaining how users should *think* about the feature, and how it should impact the way they use the middleware. It should explain the impact as concretely as possible.
- If applicable, provide sample error messages, deprecation warnings, or migration guidance.
- If applicable, explain how this feature compares to similar existing features, and in what situations the user would use each one.

# Reference-level explanation

This is the technical portion of the RFC.
Try to capture the broad implementation strategy,
and then focus in on the tricky details so that:

- Its interaction with other features is clear.
- It is reasonably clear how the feature would be implemented.
- Corner cases are dissected by example.
- Discuss how this impacts the ability to read, understand, and maintain middleware code.
  Code is read and modified far more often than written; will the proposed feature make code easier to maintain?

When necessary, this section should return to the examples given in the previous section and explain the implementation details that make them work.

When writing this section be mindful of the following:
- **RFCs should be scoped:** Try to avoid creating RFCs for huge design spaces that span many features.
  Try to pick a specific feature slice and describe it in as much detail as possible.
  Feel free to create multiple RFCs if you need multiple features.
- **RFCs should avoid ambiguity:** Two developers implementing the same RFC should come up with nearly identical implementations.
- **RFCs should be "implementable":** Merged RFCs should only depend on features from other merged RFCs and existing features.
  It is ok to create multiple dependent RFCs, but they should either be merged at the same time or have a clear merge order that ensures the "implementable" rule is respected.

# Drawbacks

Why should we *not* do this?

# Rationale and alternatives

- Why is this design the best in the space of possible designs?
- What other designs have been considered and what is the rationale for not choosing them?
- What objections immediately spring to mind? How have you addressed them?
- What is the impact of not doing this?

# \[Optional\] Prior art

Discuss prior art, both the good and the bad, in relation to this proposal.
A few examples of what this can include are:

- Does this feature exist in other markets and what experience have their community had?
- Does this feature exist in other PosSystems and what experience have their community had?
- Papers: Are there any published papers or great posts that discuss this?
  If you have some relevant papers to refer to, this can serve as a more detailed theoretical background.

This section is intended to encourage you as an author to think about the lessons from other markets and projects, provide readers of your RFC with a fuller picture.
If there is no prior art, that is fine - your ideas are interesting to us whether they are brand new or not.

Note that while precedent set by other projects and markets is some motivation, it does not on its own motivate an RFC.

# Unresolved questions

- What parts of the design do you expect to resolve through the RFC process before this gets merged?
- What parts of the design do you expect to resolve through the implementation of this feature before before the feature PR is merged?
- What related issues do you consider out of scope for this RFC that could be addressed in the future independently of the solution that comes out of this RFC?

# \[Optional\] Future possibilities

Think about what the natural extension and evolution of your proposal would be and how it would affect the middleware and ecosystem as a whole in a holistic way.
Try to use this section as a tool to more fully consider all possible interactions with the project in your proposal.
Also consider how this all fits into the roadmap for the project and of the relevant sub-team.

This is also a good place to "dump ideas", if they are out of scope for the RFC you are writing but otherwise related.

Note that having something written down in the future-possibilities section is not a reason to accept the current or a future RFC;
such notes should be in the section on motivation or rationale in this or subsequent RFCs.
The section merely provides additional information.