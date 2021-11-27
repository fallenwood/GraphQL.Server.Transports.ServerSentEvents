import './style.css';
import { createClient } from 'graphql-sse';

const messages = document.querySelector("#messages") as HTMLDivElement;

// @ts-ignore
window.subscribe = async () => {
  const client = createClient({
    singleConnection: true,  // preferred for HTTP/1 enabled servers. read more below
    url: '/graphql?id=test_by_vite',
  });


  const onNext = (message: any) => {
    console.log("New Incoming message", JSON.stringify(message));
    const div = document.createElement("div") as HTMLDivElement;
    div.innerHTML = JSON.stringify(message);
    messages.appendChild(div);
  };

  // let unsubscribe = () => {
  //   /* complete the subscription */
  // };

  client.subscribe(
    {
      query: `subscription MessageAdded {
          messageAdded {
            from { id displayName }
            content
          }
        }`,
    },
    {
      next: onNext,
      error: console.error,
      complete: () => { },
    },
  );
}