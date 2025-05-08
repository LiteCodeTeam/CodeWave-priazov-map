function formValidation ( )
{
        
        flag = true;
        pass = document.myForm.password.value;
        rpass = document.myForm.password2.value
        if (pass != rpass)
        {alert("Пароли не совпадают"); flag = false;}
        if (document.myForm.organizationName.value == "" )
        {alert("Заполните строку краткое название организации"); flag = false;}
        if (document.myForm.address.value == "" )
        {alert("Заполните строку юридический адрес"); flag = false;}
        if (document.myForm.FIO.value == "" )
        {alert("Заполните строку ФИО руководителя"); flag = false;}
        if (document.myForm.phone.value == "" )
        {alert("Заполните строку телефон руководителя"); flag = false;}
        if (document.myForm.mail.value == "" )
        {alert("Заполните строку электронная почта"); flag = false;}
        if (document.myForm.password.value == "" )
        {alert("Заполните строку пароль"); flag == false;}
        if (document.myForm.password2.value == "" )
        {alert("Заполните строку повторите пароль"); flag = false;}
        if (!flag) {
            event.preventDefault();
        } else {
            event.preventDefault();
            const body = {
                name: document.myForm.organizationName.value,
                address: document.myForm.address.value,
                FIO: document.myForm.FIO.value,
                phone: '+' + document.myForm.phone.value,
                mail: document.myForm.mail.value,
                password: document.myForm.password.value,
            }
            sendRequest('POST', requestURL, body)
                    .then(data => console.log(data))
                    .catch(err => console.log(err))
        }
}

const requestURL = 'https://jsonplaceholder.typicode.com/users'

function sendRequest(method, url, body = null){
    const headers = {
        'Content-Type': 'application/json'
    }

    return fetch(url, {
        method: method,
        body: JSON.stringify(body),
        headers: headers
    }).then(response => {
        if (response.ok){
            return response.json()
        }

        return response.json().then(error => {
            const e = new Error('Что-то пошло не так')
            e.data = error
            throw e
        })
    })    
}

// sendRequest('GET', requestURL)
//     .then(data => console.log(data))
//     .catch(err => console.log(err))



