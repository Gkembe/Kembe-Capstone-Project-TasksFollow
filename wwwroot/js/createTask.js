
document.addEventListener("DOMContentLoaded", function () {

    var form = document.querySelector("#createTaskForm, #editTaskForm");

    if (form == null) {
        return;
    }

    form.addEventListener("submit", function (event) {

        var taskTitle = document.getElementById("taskTitle");
        var description = document.getElementById("description");
        var projectId = document.getElementById("projectId");
        var status = document.getElementById("status");
        var priority = document.getElementById("priority");
        var percent = document.getElementById("percent");
        var startDate = document.getElementById("startDate");
        var dueDate = document.getElementById("dueDate");

        var isValid = true;

        // Clear previous error messages
        form.querySelectorAll(".field-validation-error, .field-validation-valid").forEach(function (span) {
            span.textContent = "";
            span.classList.remove("field-validation-error");
            span.classList.add("field-validation-valid");
        });

        // Validate task title
        if (taskTitle.value.trim() === "") {
            showError("Title", "Task title is required.");
            document.getElementById("titleStart").textContent = "*";
            document.getElementById("titleStart").style.color = "red";
            taskTitle.style.borderColor = "red";
            isValid = false;
        }
        else if (taskTitle.value.trim().length < 3) {
            showError("Title", "Task title must be at least 3 characters.");
            document.getElementById("titleStart").textContent = "*";
            document.getElementById("titleStart").style.color = "red";
            taskTitle.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("titleStart").textContent = "";
            taskTitle.style.borderColor = "";
        }

        // Validate description
        if (description.value.trim().length > 2000) {
            showError("Description", "Task description cannot be more than 2000 characters.");
            document.getElementById("descStart").textContent = "*";
            document.getElementById("descStart").style.color = "red";
            description.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("descStart").textContent = "";
            description.style.borderColor = "";
        }

        // Validate project (Create Task only)
        if (projectId != null) {

            if (projectId.value === "") {
                showError("ProjectId", "Please select a project.");
                document.getElementById("projectStart").textContent = "*";
                document.getElementById("projectStart").style.color = "red";
                projectId.style.borderColor = "red";
                isValid = false;
            }
            else {
                document.getElementById("projectStart").textContent = "";
                projectId.style.borderColor = "";
            }
        }

        // Validate status
        if (status.value !== "Not Started" && status.value !== "In Progress" && status.value !== "Completed") {
            showError("Status", "Please select a valid task status.");
            document.getElementById("statusStart").textContent = "*";
            document.getElementById("statusStart").style.color = "red";
            status.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("statusStart").textContent = "";
            status.style.borderColor = "";
        }

        // Validate priority
        if (priority.value !== "Low" && priority.value !== "Medium" && priority.value !== "High") {
            showError("Priority", "Please select a valid priority.");
            document.getElementById("priorityStart").textContent = "*";
            document.getElementById("priorityStart").style.color = "red";
            priority.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("priorityStart").textContent = "";
            priority.style.borderColor = "";
        }

        // Validate percent
        if (percent.value.trim() === "") {
            showError("Percent", "Task progress is required.");
            document.getElementById("percentStart").textContent = "*";
            document.getElementById("percentStart").style.color = "red";
            percent.style.borderColor = "red";
            isValid = false;
        }
        else if (!Number.isInteger(Number(percent.value)) || Number(percent.value) < 0 || Number(percent.value) > 100) {
            showError("Percent", "Task progress must be a whole number between 0 and 100.");
            document.getElementById("percentStart").textContent = "*";
            document.getElementById("percentStart").style.color = "red";
            percent.style.borderColor = "red";
            isValid = false;
        }
        else if (status.value === "Completed" && Number(percent.value) !== 100) {
            showError("Percent", "Completed tasks must have 100% progress.");
            document.getElementById("percentStart").textContent = "*";
            document.getElementById("percentStart").style.color = "red";
            percent.style.borderColor = "red";
            isValid = false;
        }
        else if (status.value !== "Completed" && Number(percent.value) === 100) {
            showError("Percent", "Only completed tasks can have 100% progress.");
            document.getElementById("percentStart").textContent = "*";
            document.getElementById("percentStart").style.color = "red";
            percent.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("percentStart").textContent = "";
            percent.style.borderColor = "";
        }

        // Validate start date
        if (startDate.value === "") {
            showError("StartDate", "Task start date is required.");
            document.getElementById("startDateStart").textContent = "*";
            document.getElementById("startDateStart").style.color = "red";
            startDate.style.borderColor = "red";
            isValid = false;
        }
        else if (isNaN(new Date(startDate.value).getTime())) {
            showError("StartDate", "Enter a valid start date.");
            document.getElementById("startDateStart").textContent = "*";
            document.getElementById("startDateStart").style.color = "red";
            startDate.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("startDateStart").textContent = "";
            startDate.style.borderColor = "";
        }

        // Validate due date
        if (dueDate.value === "") {
            showError("DueDate", "Task due date is required.");
            document.getElementById("dueDateStart").textContent = "*";
            document.getElementById("dueDateStart").style.color = "red";
            dueDate.style.borderColor = "red";
            isValid = false;
        }
        else if (isNaN(new Date(dueDate.value).getTime())) {
            showError("DueDate", "Enter a valid due date.");
            document.getElementById("dueDateStart").textContent = "*";
            document.getElementById("dueDateStart").style.color = "red";
            dueDate.style.borderColor = "red";
            isValid = false;
        }
        else if (startDate.value !== "" && !isNaN(new Date(startDate.value).getTime()) && dueDate.value < startDate.value) {
            showError("DueDate", "Due date cannot be earlier than start date.");
            document.getElementById("dueDateStart").textContent = "*";
            document.getElementById("dueDateStart").style.color = "red";
            dueDate.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("dueDateStart").textContent = "";
            dueDate.style.borderColor = "";
        }

        // Prevent form submission if validation fails
        if (!isValid) {
            event.preventDefault();
        }
    });

    // Display error message below the field
    function showError(fieldName, message) {
        var errorSpan = form.querySelector('[data-valmsg-for="' + fieldName + '"]');

        if (errorSpan != null) {
            errorSpan.textContent = message;
            errorSpan.classList.remove("field-validation-valid");
            errorSpan.classList.add("field-validation-error");
        }
    }
});
