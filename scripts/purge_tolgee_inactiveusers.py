HOST = "tolgee.marticliment.com"
TOKEN = ""
import requests, json, getpass

def Get_JWTToken():
    url = f"https://{HOST}/api/public/generatetoken"
    payload = json.dumps({
        "username": input("Username: "),
        "password": getpass.getpass(),
        "otp": "string"
    })
    headers = {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
    }
    response = requests.request("POST", url, headers=headers, data=payload)
    return (json.loads(response.text)["accessToken"])


def Get_AllUsers(page: int = 0) -> list[dict]:
    url = f"https://{HOST}/v2/administration/users"

    payload = {}
    query = {
        'page': str(page)
    }
    headers = {
        'Accept': 'application/json',
        'Authorization': f'Bearer {TOKEN}',
    }

    response = requests.request("GET", url, headers=headers, data=payload, params=query)

    if(response.status_code != 200):
        print(response.text)
        return []
    
    
    res_content = json.loads(response.text)
    MAX_PAGE = int(res_content["page"]["totalPages"])

    if page + 1 == MAX_PAGE:
        print(f"USER: Loaded page {page + 1} out of {MAX_PAGE}")
        return list(res_content["_embedded"]["users"])
    else:
        print(f"USER: Loaded page {page + 1} out of {MAX_PAGE}")
        return list(res_content["_embedded"]["users"]) + Get_AllUsers(page+1)

def Get_AllUsersOnProject(project: int) ->list[dict]:
    url = f"https://{HOST}/v2/projects/{project}/users"

    payload={}
    query = {
        'page': "0",
        'size': "300",
    }
    headers = {
        'Accept': 'application/json',
        'Authorization': f'Bearer {TOKEN}',
    }

    response = requests.request("GET", url, headers=headers, data=payload, params=query)

    if(response.status_code != 200):
        print(response.text)
        return []
    
    res_content = json.loads(response.text)
    print(f"PROJ: Loaded active users for project")
    return list(res_content["_embedded"]["users"])
    

def Delete_User(user_id: int) -> bool:
    url = f"https://{HOST}/v2/administration/users/{user_id}"

    payload={}
    headers = {
        'Authorization': f'Bearer {TOKEN}'
    }

    response = requests.request("DELETE", url, headers=headers, data=payload)

    if(response.status_code != 200): 
        print(response.text)
    
    return response.status_code == 200


print(f"Running for host {HOST}")

TOKEN = Get_JWTToken()

active_ids = []
for user in Get_AllUsersOnProject(1):
    active_ids.append(int(user["id"]))
active_ids.sort()

ids = []
for user in Get_AllUsers():
    ids.append(int(user["id"]))
ids.sort()

INACTIVEs = []
for user in ids:
    if user not in active_ids:
        INACTIVEs.append(user)
INACTIVEs.sort()

total = len(INACTIVEs)
SKIP_VERIFICATIION = ""
input(f"Press <ENTER> to confirm deletion of {total} users (users in server are {len(ids)}, active users are {len(active_ids)}): ")

for i in range(total):
    user = INACTIVEs[i]
    
    if(user == "1"): 
        continue
    
    if SKIP_VERIFICATIION != "12":
       SKIP_VERIFICATIION = input("About to delete User id=" + str(user) + ". Press intro to proceed, type 12 to run all: ")
    
    print(f"Deleting user id={user}, ({i+1} out of {total})... ", end="", flush=True)
    print("SUCCESS" if Delete_User(user) else "FAILED")