# DayOff Application Test Checklist

Use this checklist for a manual end-to-end review. Mark each item **Pass**, **Fail**, or **Not tested**, and record the date/build and any notes at the end.

## Before testing

- [ ] Start the app and confirm the home page loads without an exception.
- [ ] Confirm the database connection works and seeded manager and employee accounts can be used.
- [ ] Keep one browser session for an employee and another for a manager, or sign out between roles.
- [ ] Use test leave requests and test accounts. Avoid changing real account passwords during testing.

## Sign-in and account access

- [x] Sign in with a valid active employee account; confirm the Leave overview opens.
- [x] Sign in with a valid manager account; confirm the manager sees Requests, Calendar, and Team navigation.
- [ ] Enter an incorrect password; confirm sign-in fails with a clear error.
- [ ] Try signing in to an inactive account; confirm access is denied.
- [x] Confirm a signed-in user sees **Sign out** only, not **Sign in** or Razor text such as `} else {`.
- [x] Sign out; confirm the login page appears.
- [ ] Confirm protected pages require sign-in after sign-out.
- [ ] Confirm **Continue with Google** is absent from the login page.

## Forgot password and password settings

- [x] Forgot password page contains Work email, New password, and Confirm password fields.
- [ ] Submit with mismatched passwords; confirm the password is not changed and an error appears.
- [ ] Submit with a password that fails the app's Identity password policy; confirm the policy error appears.
- [ ] Submit with a valid email and matching valid password; confirm a success message appears on login.
- [ ] Sign in with the new password; confirm it works. Confirm the old password no longer works.
- [x] In account settings, confirm only Profile and Password tabs are shown.
- [ ] Change password with the correct current password; confirm success and sign in again with the new password.
- [ ] Try changing password with an incorrect current password; confirm it is rejected.

> The current training-project forgot-password flow lets anyone who knows an active account's email set a password. This is not safe for a public production app; production should require email ownership verification.

## Profile

- [x] Open Profile; confirm name, email, designation, and photo upload are available.
- [ ] Update the display name and designation; confirm the changes persist after navigating away and back.
- [x] Confirm the email is read-only.
- [ ] Upload a supported JPG, PNG, or WebP photo under 5 MB; confirm it displays after saving.
- [ ] Try an unsupported file or oversized image; confirm the upload is rejected with a helpful message.

## Leave overview and requests

- [x] Confirm the summary totals match the request data (18 total, 2 pending, 9 approved, 15 approved days off).
- [x] With more than 10 requests, confirm the initial page contains only the 10 latest submissions.
- [x] Confirm the page indicator and range show the current page (for example, 1–10 of 18; Page 1 of 2).
- [x] Choose **Next**; confirm the next page loads requests 11–18 on page 2.
- [ ] Confirm the next page has no duplicate requests from page 1.
- [ ] Choose **Previous**; confirm the prior 10 requests return.
- [x] Confirm pagination controls are visible above the table at normal browser zoom.
- [ ] Confirm bottom pagination is visible and not covered by the footer.
- [x] Change status to All, Pending, Approved, and Rejected; confirm the active filter and results match each status.
- [x] Search by employee name and leave type; confirm matching results and page 1.
- [ ] Search by email; confirm results match.
- [x] Change sort between latest submitted and earliest submitted; confirm order changes and pagination resets.
- [x] As an employee, confirm only that employee's own requests are shown.
- [x] As a manager, confirm requests across employees are shown and pending rows have Approve, Reject, and More info actions.
- [x] Confirm the manager's table has no separate “Next request to review” banner.
- [ ] Confirm rejected rows show the rejection reason when available and approved/rejected rows show decision details.

## Create and manage leave requests

- [ ] Submit a valid request for each leave type: Annual, Sick, Personal, and Parental.
- [ ] Confirm a new request appears as Pending and is included in the employee's request list.
- [ ] Try an end date before the start date; confirm validation prevents submission.
- [ ] Try a leave date in the past; confirm validation prevents submission.
- [ ] Try dates overlapping an existing non-rejected request; confirm the duplicate overlap is prevented.
- [ ] Try an annual-leave request that exceeds the allowance; confirm it is prevented with a clear message.
- [ ] As an employee, cancel a pending request; confirm it is removed.
- [ ] Try to cancel an already decided request; confirm it cannot be deleted.
- [ ] As a manager, approve a pending request; confirm its status changes to Approved and it appears on the calendar.
- [ ] As a manager, reject a pending request with a reason; confirm status and reason are visible to the employee.
- [ ] Try rejecting without a valid reason; confirm the decision is blocked.
- [ ] As a manager, request more information; confirm the request is marked Needs info and the employee can submit a response.
- [ ] After the employee responds, confirm the response appears to the manager and the manager can decide the request.
- [ ] If Undo appears after a decision, use it within the countdown; confirm the request returns to Pending.

## CSV export (manager)

- [x] Download CSV with All selected; confirm it includes all 18 requests.
- [x] Download CSV for Pending, Approved, and Rejected; confirm each file contains only that status (2 Pending, 9 Approved, 7 Rejected).
- [x] Open/parse the exported CSVs; confirm 17 columns, valid rows, and matching statuses.
- [ ] Verify quoting/escaping of commas, dates, reasons, and decision details in a spreadsheet app.
- [ ] Confirm employees cannot access the export endpoint.

## Team accounts (manager)

- [ ] Confirm the Team page lists employee name, email, annual allowance/balance, and account status.
- [ ] Add an employee with valid details; confirm the account is active and can sign in.
- [ ] Try a duplicate email or invalid password; confirm account creation is rejected with a useful error.
- [ ] Change an employee's annual allowance; confirm the saved allowance and balance update.
- [ ] Deactivate an employee; confirm the employee cannot sign in and their leave history remains.
- [ ] Reactivate the employee; confirm sign-in works again.
- [ ] Confirm the Account activity panel is absent.
- [ ] Confirm non-managers cannot access Team actions or employee management endpoints.

## Team leave calendar

- [x] Open Calendar as a manager; confirm approved leave appears and request reasons are not shown.
- [x] Confirm leave reasons are not shown on the calendar.
- [x] Use Next month; confirm October 2026 changes to November 2026.
- [ ] Verify Previous month and Today controls.
- [x] Switch between Everyone and Only me; confirm the calendar scope updates.
- [x] Open Calendar as an employee; confirm only the employee's own approved leave is shown.
- [ ] Confirm multi-day leave spans every applicable date and month boundaries display correctly.

## Responsive layout and accessibility

- [x] Check the login, overview, team, calendar, profile, and password pages at normal desktop zoom.
- [ ] Check the same pages at narrower/mobile width; confirm controls remain reachable and tables can scroll horizontally where needed. **FAIL:** At 150% zoom the overview header clips Password and Sign out. True mobile width remains untested.
- [ ] Check pagination at normal zoom; confirm bottom controls are not covered by the app footer. (Not tested.)
- [ ] Navigate links, forms, dropdowns, and modals using the keyboard; confirm focus is visible.
- [ ] Confirm labels and validation messages are readable and associated with their fields.

## Browser check run — 2026-10-25

Read-only checks completed in Chrome at `localhost:5288`:

- **Pass:** Employee session opened Leave overview; the employee navigation showed Requests, Calendar, Password, and Sign out.
- **Pass:** Employee overview displayed only that employee's four requests and showed `Page 1 of 1`.
- **Pass:** Employee Calendar rendered approved leave for the employee, showed no approved leave today, and kept request reasons private.
- **Pass:** Manager demo sign-in opened Leave overview with Requests, Calendar, Team, and Sign out navigation.
- **Pass:** Manager Requests showed 18 requests, 10 per page, range `1–10 of 18`, and `Page 1 of 2`.
- **Pass:** Next loaded `11–18 of 18` on page 2; Pending filter returned exactly two pending requests on page 1 of 1.
- **Pass:** Manager Calendar showed Everyone and Only me controls, approved leave, and the team summary (`5 people have leave booked next week`).
- **Pass:** Team page loaded and showed three active employee accounts; no account actions were used.
- **Pass:** Password settings showed current, new, and confirmation fields; Profile showed photo upload, editable name, read-only email, and designation.
- **Not tested:** Creating/editing requests or accounts, approving/rejecting, actual password changes/reset, photo upload, invalid-input behavior, a true mobile-sized window, and accessibility checks. These require test data or make persistent changes.
## Browser check follow-up — 2026-10-25

Additional read-only checks:

- **Pass:** All CSV export parsed as 18 rows with 17 columns; statuses were 9 Approved, 2 Pending, and 7 Rejected.
- **Pass:** Approved CSV contained 9 Approved rows; Pending CSV contained 2 Pending rows; Rejected CSV contained 7 Rejected rows. Each filtered file parsed with 17 columns and no rows from another status.
- **Pass:** Searching by employee name narrowed results to 6; searching by leave type narrowed the current Rejected filter to 3 Annual leave requests. Search results reset to page 1.
- **Pass:** Earliest-first sorting updated the URL sort parameter and showed the earlier rejected decisions first while retaining the status filter.
- **Pass:** Manager calendar advanced October 2026 to November 2026. Selecting Only me retained November and changed the team summary to the manager's own scope.
- **Pass:** Forgot password page displayed Work email, New password, Confirm password, Update password, and Back to sign in. No password was submitted.
- **Fail:** At 150% browser zoom (narrow effective viewport), the header clipped Password and Sign out from view. This suggests the narrow layout needs a responsive navigation fix. A true mobile-sized window was not tested.
- **Not tested:** Leave-request create/validation/cancel/approval/rejection/more-info flows, profile photo upload/validation, and account changes. These can persist records or send notices; use disposable accounts/data before exercising them.
## Completion notes

- Test date/build: 2026-10-25 / local browser run
- Tested by: Codex
- Failed items / notes: Overview header clips Password and Sign out at 150% zoom.
- _______________________________________________







