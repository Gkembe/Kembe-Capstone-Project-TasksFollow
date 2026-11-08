document.addEventListener("DOMContentLoaded", function () {

    var searchInput = document.getElementById("searchIn");
    var companies = document.querySelectorAll(".allRow");

    searchInput.addEventListener("input", function () {

        var searchValue = searchInput.value.toLowerCase().trim();

        for (var i = 0; i < companies.length; i++) {

            var companyText = companies[i].textContent.toLowerCase();

            if (companyText.includes(searchValue)) {
                companies[i].style.display = "";
            }
            else {
                companies[i].style.display = "none";
            }
        }
    });
});