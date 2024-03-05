var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/Admin/User/GetAll' }, // Retrieves all the record

        // Formats the records
        "columns": [
            { "data": "name", "width": "10%" },           
            { "data": "role", "width": "15%" },
            { "data": "email", "width": "15%" },
            { "data": "phoneNumber", "width": "15%" },
            { "data": "company.name", "width": "15%" },
            {
                data: { id: "id", lockoutEnd: "lockoutEnd" },
                "render": function (data) {
                    var today = new Date().getTime();
                    var lockout = new Date(data.lockoutEnd).getTime();

                    if (lockout > today) {
                        return `
                        <div class="btn-group text-center" role="group">
                             <a onclick=LockUnlock('${data.id}') class="btn btn-danger text-white mx-2" style="cursor:pointer; width:100px;">
                                    <i class="bi bi-lock-fill"></i>  Lock
                                </a> 
                                <a href="/admin/user/RoleManagment?userId=${data.id}" class="btn btn-danger text-white mx-2" style="cursor:pointer; width:150px;">
                                     <i class="bi bi-pencil-square"></i> Edit
                                </a>
                        </div>`

                    }
                    else {
                        return `
                        <div class="btn-group text-center" role="group">
                              <a onclick=LockUnlock('${data.id}') class="btn btn-success text-white mx-2" style="cursor:pointer; width:100px;">
                                    <i class="bi bi-unlock-fill"></i>  UnLock
                                </a>
                                <a href="/admin/user/RoleManagment?userId=${data.id}" class="btn btn-danger text-white mx-2" style="cursor:pointer; width:150px;">
                                     <i class="bi bi-pencil-square"></i> Edit
                                </a>
                        </div>`
                    }


                },
                "width": "25%"
            }
        ]
    });
}


function LockUnlock(id) {
    $.ajax({
        type: "POST",
        url: '/Admin/User/LockUnlock', // Allows us to use the API call in the UserController script
        data: JSON.stringify(id), // We need to stringify the id so that it can be read
        contentType: "application/json",
        success: function (data) {
            if (data.success) {
                toastr.success(data.message);
                dataTable.ajax.reload();
            }
        }
    });
}