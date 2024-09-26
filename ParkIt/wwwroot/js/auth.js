// Global AJAX setup for adding Authorization header

console.log("entered auth.js");
$.ajaxSetup({
    beforeSend: function (xhr) {
        const token = localStorage.getItem('jwtToken');  // Retrieve the JWT token
        console.log(token);
        if (token) {
            // If the token exists, set the Authorization header
            xhr.setRequestHeader('Authorization', 'Bearer ' + token);
        }
    },
    error: function (xhr, status, error) {
        if (xhr.status === 401) {
            // Handle unauthorized access (e.g., token expired or invalid)
            console.log('Unauthorized: Redirecting to login page');
            window.location.href = '/Login/Login';  // Redirect to login page
        } else {
            console.log('Error:', error);  // Handle other errors
        }
    }
});
