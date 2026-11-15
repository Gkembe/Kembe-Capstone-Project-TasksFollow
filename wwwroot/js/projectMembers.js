
document.addEventListener("DOMContentLoaded", function () {

    var form = document.getElementById("addMemberForm");

    form.addEventListener("submit", function (event) {

        var employeeId = document.getElementById("employeeId");
        var isValid = true;

        // Clear previous error messages
        form.querySelectorAll(".field-validation-error, .field-validation-valid").forEach(function (span) {
            span.textContent = "";
        });

        // Validate selected employee
        if (employeeId.value === "") {
            showError("employeeId", "Please select an employee.");
            document.getElementById("employeeStart").textContent = "*";
            document.getElementById("employeeStart").style.color = "red";
            employeeId.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("employeeStart").textContent = "";
            employeeId.style.borderColor = "";
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

    // Confirm before removing a project member
    var removeForms = document.querySelectorAll(".removeMemberForm");

    removeForms.forEach(function (removeForm) {

        removeForm.addEventListener("submit", function (event) {

            var confirmation = confirm("Remove this employee from the project?");

            if (!confirmation) {
                event.preventDefault();
            }
        });
    });
});
